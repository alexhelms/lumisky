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
using OdinEye.Core.Utilities;
using Quartz;
using SlimMessageBus;
using System.Diagnostics;

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

    public string RawImageTempFilename { get; set; } = string.Empty;

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
        Log.Information($"Processing {RawImageTempFilename}");

        var rawImageFilInfo = new FileInfo(RawImageTempFilename);
        if (!rawImageFilInfo.Exists)
        {
            Log.Error("File for processing not found {Filename}", rawImageFilInfo.FullName);
            return;
        }

        if (rawImageFilInfo.Length == 0)
        {
            Log.Error("{Filename} is zero length", rawImageFilInfo.FullName);
            try { rawImageFilInfo.Delete(); } catch { }
            return;
        }

        var startTime = Stopwatch.GetTimestamp();

        using var processResult = _imageService.ProcessFits(rawImageFilInfo.FullName);

        // Process, Save, and Persis the Image
        await DrawCardinalOverlayForImage(processResult.Image);
        var imageFilename = SaveImage(processResult.Image, "image", processResult.ExposureUtc);
        await PersistImage(imageFilename, processResult.ExposureUtc);

        // Process, Save, and Persis the Panorama
        string? panoramaFilename = null;
        if (processResult.Panorama is not null)
        {
            await DrawCardinalOverlayForPanorama(processResult.Panorama);
            panoramaFilename = SaveImage(processResult.Panorama, "panorama", processResult.ExposureUtc);
            await PersistPanorama(panoramaFilename, processResult.ExposureUtc);
        }

        // Process, Save, and Persis the Raw Image
        string? rawImageFilename = null;
        if (_profile.Current.Image.KeepRawImages)
        {
            rawImageFilename = SaveRawImage(rawImageFilInfo.FullName, processResult.ExposureUtc);
            await PersistRawImage(rawImageFilename, processResult.ExposureUtc);
        }
        else
        {
            rawImageFilInfo.Delete();
        }

        _exposureTrackingService.AddMostRecentStatistics(
            exposure: processResult.ExposureDuration,
            median: processResult.Median,
            gain: processResult.Gain);

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

        if (processTimeElapsed > (_profile.Current.Capture.CaptureInterval - processResult.ExposureDuration))
        {
            Log.Warning("Processing time exceeds available time between exposures. Consider reducing your max exposure time.");
        }

        await context.Scheduler.TriggerJob(
            ExportJob.Key,
            new JobDataMap
            {
                [nameof(ExportJob.RawFilename)] = rawImageFilename!,
                [nameof(ExportJob.ImageFilename)] = imageFilename,
                [nameof(ExportJob.PanoramaFilename)] = panoramaFilename!,
            });
    }

    private string SaveRawImage(string tempFilename, DateTime timestamp)
    {
        var filename = _filenameGenerator.CreateFilename("raw", timestamp, ".fits");
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
        File.Move(tempFilename, filename, overwrite: true);
        return filename;
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

    private async Task<int> PersistRawImage(string filename, DateTime timestamp)
    {
        var rawImage = new Data.RawImage
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(timestamp).ToUnixTimeSeconds(),
        };

        _dbContext.RawImages.Add(rawImage);
        await _dbContext.SaveChangesAsync();
        return rawImage.Id;
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

        Log.Information("Added image {Filename}", filename);
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

        Log.Information("Added panorama {Filename}", filename);
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
                _profile.Current.Processing.PanoramaCardinal0AzimuthString,
                _profile.Current.Processing.PanoramaCardinal90AzimuthString,
                _profile.Current.Processing.PanoramaCardinal180AzimuthString,
                _profile.Current.Processing.PanoramaCardinal270AzimuthString,
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
