using OdinEye.Core.Devices;
using OdinEye.Core.Imaging;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;

namespace OdinEye.Core.Jobs;

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
        IndiCamera? camera = null;

        try
        {
            camera = await CreateCamera(context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();

            using var image = await ExposeImage(camera, context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();

            var filename = SaveImage(image);
            context.CancellationToken.ThrowIfCancellationRequested();

            await context.Scheduler.TriggerJob(
                ProcessingJob.Key,
                new JobDataMap
                {
                    [nameof(ProcessingJob.RawImageTempFilename)] = filename,
                });
        }
        finally
        {
            if (camera is not null)
            {
                try
                {
                    await camera.DisconnectAsync();
                    camera.Dispose();
                    camera = null;
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Error disconnecting from camera");
                    throw;
                }
            }
        }
    }

    private async Task<IndiCamera> CreateCamera(CancellationToken token)
    {
        var camera = _deviceFactory.CreateCamera();
        if (camera is null)
            throw new NullReferenceException("Could not create camera.");

        bool connected = await camera.ConnectAsync(token);
        if (!connected)
            throw new NotConnectedException($"{camera.Name} failed to connect. Check settings and try again.");

        return camera;
    }

    private async Task<AllSkyImage> ExposeImage(IndiCamera camera, CancellationToken token)
    {
        bool isDay = _sunService.IsDaytime;
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
        var filename = Path.Join(Path.GetTempPath(), "odineye", $"raw_{Guid.NewGuid():N}.fits");
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);

        image.SaveAsFits(filename, ImageOutputType.UInt16);
        return filename;
    }
}
