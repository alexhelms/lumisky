using LumiSky.Core.Devices;
using LumiSky.Core.Imaging;
using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Quartz;

namespace LumiSky.Core.Jobs;

[DisallowConcurrentExecution]
public class CaptureJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Capture, JobConstants.Groups.Allsky);

    private readonly IProfileProvider _profile;
    private readonly DeviceFactory _deviceFactory;
    private readonly SunService _sunService;
    private readonly ExposureService _exposureTrackingService;

    public CaptureJob(
        IProfileProvider profile,
        DeviceFactory deviceFactory,
        SunService dayNightService,
        ExposureService exposureTrackingService)
    {
        _profile = profile;
        _deviceFactory = deviceFactory;
        _sunService = dayNightService;
        _exposureTrackingService = exposureTrackingService;

        RetryJobOnException = true;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        IndiCamera camera = await _deviceFactory.GetOrCreateConnectedCamera(context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();

        using var image = await ExposeImage(camera, context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();

        var filename = SaveImage(image);
        context.CancellationToken.ThrowIfCancellationRequested();

        var elapsedJobTime = context.FireTimeUtc - DateTime.UtcNow;
        if (elapsedJobTime > _profile.Current.Capture.CaptureInterval)
        {
            var suggestedMaxExposureSeconds = Math.Ceiling((elapsedJobTime - _profile.Current.Capture.CaptureInterval).TotalSeconds);
            Log.Warning(
                "Total capture job time ({Elapsed:F3}s) exceeds capture interval ({Interval:F3}s). " +
                "Consider reducing your max exposure time by {Suggestion:F0} seconds.",
                elapsedJobTime.TotalSeconds, _profile.Current.Capture.CaptureInterval, suggestedMaxExposureSeconds);
        }

        await context.Scheduler.TriggerJob(
            ProcessingJob.Key,
            new JobDataMap
            {
                [nameof(ProcessingJob.RawImageTempFilename)] = filename,
            });
    }


    private async Task<AllSkyImage> ExposeImage(IndiCamera camera, CancellationToken token)
    {
        bool isDay = _sunService.IsDaytime();
        var exposureParameters = new ExposureParameters
        {
            Duration = _exposureTrackingService.GetNextExposure(),
            Gain = isDay ? _profile.Current.Camera.DaytimeGain : _profile.Current.Camera.NighttimeGain,
            Offset = _profile.Current.Camera.Offset,
        };

        Log.Information("Capturing image {Exposure:F6} sec, {Gain} gain, {Offset} offset",
            exposureParameters.Duration.TotalSeconds,
            exposureParameters.Gain,
            exposureParameters.Offset);

        var image = await camera.TakeImageAsync(exposureParameters, token);
        token.ThrowIfCancellationRequested();
        if (image is null)
            throw new NullReferenceException("Image capture failed.");

        var (alt, _) = _sunService.GetPosition(image.Metadata.ExposureUtc ?? DateTime.UtcNow);
        image.Metadata.SunAltitude = alt;

        return image;
    }

    private string SaveImage(AllSkyImage image)
    {
        // Save the raw image to a temporary path. The processing job will move it as needed.
        var filename = Path.Join(Path.GetTempPath(), "lumisky", $"raw_{Guid.NewGuid():N}.fits");
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);

        image.SaveAsFits(filename, ImageOutputType.UInt16);
        return filename;
    }
}
