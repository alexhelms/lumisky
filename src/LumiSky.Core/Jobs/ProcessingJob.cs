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
using LumiSky.Core.Utilities;

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

        ProcessTimingTracker.Clear();

        using var processResult = _imageService.ProcessFits(rawImageFileInfo.FullName);
        var exposureUtc = processResult.Metadata.ExposureUtc ?? DateTime.UtcNow;

        string? imageFilename = null;
        string? panoramaFilename = null;
        string? rawImageFilename = null;

        var imageTask = Task.Run(async () =>
        {
            // Process, Save, and Persis the Image
            await DrawImageOverlays(processResult.Image, processResult.Metadata);
            imageFilename = SaveImage(processResult.Image, "image", exposureUtc);
            await PersistImage(imageFilename, exposureUtc);
        });

        var panoramaTask = Task.Run(async () =>
        {
            // Process, Save, and Persis the Panorama
            if (processResult.Panorama is not null)
            {
                await DrawPanoramaOverlays(processResult.Panorama);
                panoramaFilename = SaveImage(processResult.Panorama, "panorama", exposureUtc);
                await PersistPanorama(panoramaFilename, exposureUtc);
            }
        });

        var rawTask = Task.Run(async () =>
        {
            // Process, Save, and Persis the Raw Image
            if (_profile.Current.Image.KeepRawImages)
            {
                rawImageFilename = SaveRawImage(rawImageFileInfo.FullName, exposureUtc);
                await PersistRawImage(rawImageFilename, exposureUtc);
            }
            else
            {
                rawImageFileInfo.Delete();
            }
        });

        await Task.WhenAll(imageTask, panoramaTask, rawTask);

        if (imageFilename is null)
        {
            // This shouldn't happen.
            throw new JobExecutionException("Error saving image file");
        }

        _exposureTrackingService.AddMostRecentStatistics(
            exposure: processResult.Metadata.ExposureDuration.GetValueOrDefault(),
            median: processResult.Median,
            gain: processResult.Metadata.Gain.GetValueOrDefault());

        var processTimeElapsed = Stopwatch.GetElapsedTime(startTime);

        ProcessTimingTracker.FireComplete();

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

        if (processTimeElapsed > _profile.Current.Capture.CaptureInterval)
        {
            Log.Warning("Processing duration ({Elapsed:F3}s) exceeds capture interval ({Interval:F1}s).",
                processTimeElapsed.TotalSeconds, _profile.Current.Capture.CaptureInterval.TotalSeconds);
            await _bus.Publish(new NotificationMessage
            {
                Type = NotificationType.Warning,
                Summary = "Processing duratioon exceeded capture interval.",
                Detail = "Procesing is taking too much time. You should increase the capture interval " +
                         "until it is longer than the time it takes to process the previous image.",
            });
        }

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

        // LumiSky often runs on limited resource systems, like a raspi.
        // Forcing a GC collection after all the the heavy image processing
        // is probably for the best.
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
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
        var rawImage = new RawImage
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
        var newImage = new Image
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
        var newImage = new Panorama
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
        using var _ = Benchmark.Start(t => ProcessTimingTracker.Add(new("Draw Overlays", t)));
        var renderer = new OverlayRenderer(_profile);
        await renderer.DrawImageOverlays(image, metadata);
    }

    private async Task DrawPanoramaOverlays(Mat panorama)
    {
        // The cardinal overlay is the only overlay on a panorama.

        if (_profile.Current.Processing.DrawCardinalOverlay)
        {
            using var _ = Benchmark.Start(t => ProcessTimingTracker.Add(new("Draw Panorama Overlays", t)));
            var renderer = new OverlayRenderer(_profile);
            await renderer.DrawPanoramaOverlays(panorama);
        }
    }
}
