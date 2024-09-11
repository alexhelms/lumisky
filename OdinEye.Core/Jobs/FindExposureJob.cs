using OdinEye.Core.Data;
using OdinEye.Core.Devices;
using OdinEye.Core.Imaging.Processing;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;
namespace OdinEye.Core.Jobs;

[DisallowConcurrentExecution]
public class FindExposureJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.FindExposure, JobConstants.Groups.Allsky);

    private readonly IProfileProvider _profile;
    private readonly DeviceFactory _deviceFactory;
    private readonly ExposureService _exposureService;
    private readonly SunService _sunService;

    public FindExposureJob(
        IProfileProvider profile,
        DeviceFactory deviceFactory,
        ExposureService exposureService,
        SunService sunService)
    {
        _profile = profile;
        _deviceFactory = deviceFactory;
        _exposureService = exposureService;
        _sunService = sunService;
    }

    protected async override Task OnExecute(IJobExecutionContext context)
    {
        IndiCamera? camera = null;
        bool success = false;

        try
        {
            camera = await CreateCamera(context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();
            
            bool isDay = _sunService.IsDaytime;
            var exposure = isDay
                ? TimeSpan.FromSeconds(1)
                : TimeSpan.FromSeconds(5);

            var gain = isDay ?
                _profile.Current.Camera.DaytimeGain
                : _profile.Current.Camera.NighttimeGain;

            var exposureParameters = new ExposureParameters
            {
                Duration = exposure,
                Gain = gain,
                Offset = _profile.Current.Camera.Offset,
            };

            Log.Information("Finding initial exposure");

            while (true)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var median = await ExposeAndMeasureMedian(camera, exposureParameters, context.CancellationToken);
                _exposureService.AddMostRecentStatistics(exposureParameters.Duration, median, gain);

                var nextExposure = _exposureService.GetNextExposure();
                exposureParameters = exposureParameters with { Duration = nextExposure };

                if (median < 0.9)
                {
                    Log.Information("Starting exposure found at {Exposure:#.000000} sec, iterating once more",
                        exposureParameters.Duration.TotalSeconds);

                    median = await ExposeAndMeasureMedian(camera, exposureParameters, context.CancellationToken);
                    _exposureService.AddMostRecentStatistics(exposureParameters.Duration, median, gain);

                    Log.Information("Final starting exposure is {Exposure:#.000000} sec",
                        _exposureService.GetNextExposure().TotalSeconds);

                    success = true;
                    break;
                }
            }
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

        context.CancellationToken.ThrowIfCancellationRequested();

        if (success)
        {
            var trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerKeys.ScheduledAllsky)
                .ForJob(CaptureJob.Key)
                .WithSimpleSchedule(o => o
                    .WithInterval(_profile.Current.Capture.CaptureInterval)
                    .RepeatForever())
                .Build();

            await context.Scheduler.ScheduleJob(trigger);
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

    private async Task<double> ExposeAndMeasureMedian(
        IndiCamera camera,
        ExposureParameters exposureParameters,
        CancellationToken token)
    {
        Log.Information("Exposing {Exposure:#.000000} sec gain {Gain}",
                    exposureParameters.Duration.TotalSeconds, exposureParameters.Gain);

        using var image = await camera.TakeImageAsync(exposureParameters, token);
        token.ThrowIfCancellationRequested();
        if (image is null)
            throw new NullReferenceException("Image capture failed.");

        using var debayeredImage = Debayer.FromImage(image);
        double greenMedian = debayeredImage.Median(channel: 1);
        Log.Information("Median {Median:F6}", greenMedian);
        return greenMedian;
    }
}
