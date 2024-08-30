using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.EntityFrameworkCore;
using OdinEye.Core.Data;
using OdinEye.Core.DomainEvents;
using OdinEye.Core.Imaging;
using OdinEye.Core.Imaging.Processing;
using OdinEye.Core.Primitives;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;
using SlimMessageBus;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace OdinEye.Core.Jobs;

public class ProcessingJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Processing, JobConstants.Groups.Allsky);
    
    private readonly IProfileProvider _profile;
    private readonly IMessageBus _bus;
    private readonly AppDbContext _dbContext;
    private readonly ImageService _imageService;
    private readonly FilenameGenerator _filenameGenerator;
    private readonly ExposureService _exposureTrackingService;

    public int RawImageId { get; set; }

    public ProcessingJob(
        IProfileProvider profile,
        IMessageBus bus,
        AppDbContext dbContext,
        ImageService imageService,
        FilenameGenerator filenameGenerator,
        ExposureService exposureTrackingService)
    {
        _profile = profile;
        _bus = bus;
        _dbContext = dbContext;
        _imageService = imageService;
        _filenameGenerator = filenameGenerator;
        _exposureTrackingService = exposureTrackingService;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        var rawImage = await _dbContext.RawImages
            .AsNoTracking()
            .FirstAsync(x => x.Id == RawImageId);

        Log.Information($"Processing {rawImage.Filename}");

        var rawImageFilInfo = new FileInfo(rawImage.Filename);
        if (!rawImageFilInfo.Exists)
        {
            Log.Error("File for processing not found {Filename}", rawImageFilInfo.FullName);
            return;
        }

        if (rawImageFilInfo.Length == 0)
        {
            Log.Error("{Filename} is zero length", rawImage.Filename);
            try { rawImageFilInfo.Delete(); } catch { }
            return;
        }

        var startTime = Stopwatch.GetTimestamp();

        using var processResult = _imageService.ProcessFits(rawImage.Filename);
        await DrawCardinalOverlayForImage(processResult.Image);
        var imageFilename = SaveImage(processResult.Image, "image", processResult.ExposureUtc);
        await PersistImage(imageFilename, processResult.ExposureUtc);

        string? panoramaFilename = null;
        if (processResult.Panorama is not null)
        {
            await DrawCardinalOverlayForPanorama(processResult.Panorama);
            panoramaFilename = SaveImage(processResult.Panorama, "panorama", processResult.ExposureUtc);
            await PersistPanorama(panoramaFilename, processResult.ExposureUtc);
        }

        _exposureTrackingService.AddMostRecentStatistics(
            exposure: processResult.ExposureDuration,
            median: processResult.Median,
            gain: processResult.Gain);

        // TODO: If "ExportFits" is enabled, send to ExportJob
        // TODO: Send final image file to ExportJob

        if (!_profile.Current.Image.KeepRawImages)
        {
            await DeleteRawImage();
        }

        var processTimeElapsed = Stopwatch.GetElapsedTime(startTime);
        
        await _bus.Publish(new NewImageEvent
        {
            Filename = imageFilename,
            Size = (Size)processResult.Image.Size,
        });

        if (panoramaFilename is not null && processResult.Panorama is not null)
            await _bus.Publish(new NewPanoramaEvent
            { 
                Filename = panoramaFilename,
                Size = (Size)processResult.Panorama.Size,
            });

        Log.Information("Processing completed in {Elapsed:F3} seconds", processTimeElapsed.TotalSeconds);

        await context.Scheduler.TriggerJob(
            ExportJob.Key,
            new JobDataMap
            {
                [nameof(ExportJob.RawFilename)] = rawImage.Filename,
                [nameof(ExportJob.ImageFilename)] = imageFilename,
                [nameof(ExportJob.PanoramaFilename)] = panoramaFilename!,
            });
    }

    private string SaveImage(Mat uint8Mat, string imageType, DateTime timestamp)
    {
        var filename = _filenameGenerator.CreateFilename(imageType, timestamp, _filenameGenerator.ImageExtension);
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);

        var encoderParameters = new List<KeyValuePair<ImwriteFlags, int>>();
        if (_profile.Current.Image.FileType == ImageFileType.JPEG)
        {
            int quality = Math.Clamp(_profile.Current.Image.JpegQuality, 0, 100);
            encoderParameters.Add(new(ImwriteFlags.JpegQuality, quality));
        }
        else if (_profile.Current.Image.FileType == ImageFileType.PNG)
        {
            int compression = Math.Clamp(_profile.Current.Image.PngCompression, 0, 9);
            encoderParameters.Add(new(ImwriteFlags.PngCompression, compression));
        }

        using var image = new Image<Rgb, byte>(uint8Mat);
        bool saved = CvInvoke.Imwrite(filename, image, encoderParameters.ToArray());
        if (!saved)
            throw new Exception("Image failed to save");

        return filename;
    }

    private async Task PersistImage(string filename, DateTime exposureUtc)
    {
        var newImage = new Data.Image
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(exposureUtc).ToUnixTimeSeconds(),
        };

        _dbContext.Images.Add(newImage);
        await _dbContext.SaveChangesAsync();
    }

    private async Task PersistPanorama(string filename, DateTime exposureUtc)
    {
        var newImage = new Data.Panorama
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(exposureUtc).ToUnixTimeSeconds(),
        };

        _dbContext.Panoramas.Add(newImage);
        await _dbContext.SaveChangesAsync();
    }

    private async Task DeleteRawImage()
    {
        string? filename = null;

        try
        {
            var rawImage = await _dbContext.RawImages.FirstAsync(x => x.Id == RawImageId);
            filename = rawImage.Filename;
            _dbContext.RawImages.Remove(rawImage);
            await _dbContext.SaveChangesAsync();
            var fileInfo = new FileInfo(rawImage.Filename);
            if (fileInfo.Exists)
            {
                // Delete the raw file
                fileInfo.Delete();
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Error deleting raw image file {Filename}", filename);
        }
    }

    private async Task DrawCardinalOverlayForImage(Mat image)
    {
        if (_profile.Current.Processing.DrawCardinalOverlay)
        {
            string[] labels = [
                _profile.Current.Processing.CardinalTopString,
                _profile.Current.Processing.CardinalBottomString,
                _profile.Current.Processing.CardinalRightString,
                _profile.Current.Processing.CardinalLeftString,
            ];

            try
            {
                await Overlay.DrawCardinalPoints(image,
                    labels,
                    fontSize: _profile.Current.Processing.TextSize,
                    fill: _profile.Current.Processing.TextColor,
                    strokeFill: _profile.Current.Processing.TextOutlineColor,
                    strokeWidth: _profile.Current.Processing.TextOutline,
                    margin: _profile.Current.Processing.TextSize / 2);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error drawing cardinal point overlay on image");
            }
        }
    }

    private async Task DrawCardinalOverlayForPanorama(Mat panorama)
    {
        if (_profile.Current.Processing.DrawCardinalOverlay)
        {
            string[] labels = [
                _profile.Current.Processing.CardinalTopString,
                _profile.Current.Processing.CardinalBottomString,
                _profile.Current.Processing.CardinalRightString,
                _profile.Current.Processing.CardinalLeftString,
            ];

            try
            {
                await Overlay.DrawCardinalPointsPanorama(panorama,
                    labels,
                    fontSize: _profile.Current.Processing.TextSize,
                    fill: _profile.Current.Processing.TextColor,
                    strokeFill: _profile.Current.Processing.TextOutlineColor,
                    strokeWidth: _profile.Current.Processing.TextOutline,
                    margin: _profile.Current.Processing.TextSize / 2);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error drawing cardinal point overlay on panorama");
            }
        }
    }
}
