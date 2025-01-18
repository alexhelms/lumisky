using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.EntityFrameworkCore;
using LumiSky.Core.Data;
using LumiSky.Core.DomainEvents;
using LumiSky.Core.Imaging;
using LumiSky.Core.Imaging.Processing;
using LumiSky.Core.Primitives;
using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Quartz;
using SlimMessageBus;
using System.Diagnostics;

namespace LumiSky.Core.Jobs;

public class ProcessingJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Processing, JobConstants.Groups.Allsky);
    
    private readonly IProfileProvider _profile;
    private readonly IMessageBus _bus;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ImageService _imageService;
    private readonly FilenameGenerator _filenameGenerator;
    private readonly ExposureService _exposureTrackingService;

    public string RawImageTempFilename { get; set; } = string.Empty;

    public ProcessingJob(
        IProfileProvider profile,
        IMessageBus bus,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ImageService imageService,
        FilenameGenerator filenameGenerator,
        ExposureService exposureTrackingService)
    {
        _profile = profile;
        _bus = bus;
        _dbContextFactory = dbContextFactory;
        _imageService = imageService;
        _filenameGenerator = filenameGenerator;
        _exposureTrackingService = exposureTrackingService;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        Log.Information($"Processing {RawImageTempFilename}");

        var rawImageFileInfo = new FileInfo(RawImageTempFilename);
        if (!rawImageFileInfo.Exists)
        {
            Log.Error("File for processing not found {Filename}", rawImageFileInfo.FullName);
            return;
        }

        if (rawImageFileInfo.Length == 0)
        {
            Log.Error("{Filename} is zero length", rawImageFileInfo.FullName);
            try { rawImageFileInfo.Delete(); } catch { }
            return;
        }

        var startTime = Stopwatch.GetTimestamp();

        using var processResult = _imageService.ProcessFits(rawImageFileInfo.FullName);
        var exposureUtc = processResult.Metadata.ExposureUtc ?? DateTime.UtcNow;

        // Process, Save, and Persis the Image
        await DrawImageOverlays(processResult.Image, processResult.Metadata);
        var imageFilename = SaveImage(processResult.Image, "image", exposureUtc);
        await PersistImage(imageFilename, exposureUtc);

        // Process, Save, and Persis the Panorama
        string? panoramaFilename = null;
        if (processResult.Panorama is not null)
        {
            await DrawPanoramaOverlays(processResult.Panorama);
            panoramaFilename = SaveImage(processResult.Panorama, "panorama", exposureUtc);
            await PersistPanorama(panoramaFilename, exposureUtc);
        }

        // Process, Save, and Persis the Raw Image
        string? rawImageFilename = null;
        if (_profile.Current.Image.KeepRawImages)
        {
            rawImageFilename = SaveRawImage(rawImageFileInfo.FullName, exposureUtc);
            await PersistRawImage(rawImageFilename, exposureUtc);
        }
        else
        {
            rawImageFileInfo.Delete();
        }

        _exposureTrackingService.AddMostRecentStatistics(
            exposure: processResult.Metadata.ExposureDuration.GetValueOrDefault(),
            median: processResult.Median,
            gain: processResult.Metadata.Gain.GetValueOrDefault());

        var processTimeElapsed = Stopwatch.GetElapsedTime(startTime);
        
        await _bus.Publish(new NewImageEvent
        {
            Filename = imageFilename,
        });

        if (panoramaFilename is not null && processResult.Panorama is not null)
            await _bus.Publish(new NewPanoramaEvent
            { 
                Filename = panoramaFilename
            });

        Log.Information("Processing completed in {Elapsed:F3} seconds", processTimeElapsed.TotalSeconds);

        context.CancellationToken.ThrowIfCancellationRequested();

        await context.Scheduler.TriggerJob(
            ExportJob.Key,
            new JobDataMap
            {
                [nameof(ExportJob.RawFilename)] = rawImageFilename!,
                [nameof(ExportJob.ImageFilename)] = imageFilename,
                [nameof(ExportJob.PanoramaFilename)] = panoramaFilename!,
            });

        await context.Scheduler.TriggerJob(
            PublishJob.Key,
            new JobDataMap
            {
                [nameof(PublishJob.ImageFilename)] = imageFilename,
                [nameof(PublishJob.PanoramaFilename)] = panoramaFilename!,
            });
    }

    private string SaveRawImage(string tempFilename, DateTime timestamp)
    {
        var filename = _filenameGenerator.CreateImageFilename("raw", timestamp, ".fits");
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
        File.Move(tempFilename, filename, overwrite: true);
        return filename;
    }

    private string SaveImage(Mat uint8Mat, string imageType, DateTime timestamp)
    {
        var filename = _filenameGenerator.CreateImageFilename(imageType, timestamp, _filenameGenerator.ImageExtension);
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

    private async Task PersistRawImage(string filename, DateTime exposureUtc)
    {
        var rawImage = new Data.RawImage
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(exposureUtc).ToUnixTimeSeconds(),
        };

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.RawImages.Add(rawImage);
        await dbContext.SaveChangesAsync();

        Log.Information("Added raw image {Filename}", filename);
    }

    private async Task PersistImage(string filename, DateTime exposureUtc)
    {
        var newImage = new Data.Image
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(exposureUtc).ToUnixTimeSeconds(),
        };

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Images.Add(newImage);
        await dbContext.SaveChangesAsync();

        Log.Information("Added image {Filename}", filename);
    }

    private async Task PersistPanorama(string filename, DateTime exposureUtc)
    {
        var newImage = new Data.Panorama
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(exposureUtc).ToUnixTimeSeconds(),
        };

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Panoramas.Add(newImage);
        await dbContext.SaveChangesAsync();

        Log.Information("Added panorama {Filename}", filename);
    }

    private async Task DrawImageOverlays(Mat image, ImageMetadata metadata)
    {
        var renderer = new OverlayRenderer(_profile);
        await renderer.DrawImageOverlays(image, metadata);
    }

    private async Task DrawPanoramaOverlays(Mat panorama)
    {
        if (_profile.Current.Processing.DrawCardinalOverlay)
        {
            var renderer = new OverlayRenderer(_profile);
            await renderer.DrawPanoramaOverlays(panorama);
        }
    }
}
