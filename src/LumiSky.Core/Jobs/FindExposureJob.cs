using LumiSky.Core.Data;
using LumiSky.Core.Devices;
using LumiSky.Core.Imaging.Processing;
using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Quartz;
namespace LumiSky.Core.Jobs;

[DisallowConcurrentExecution]
public class FindExposureJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.FindExposure, JobConstants.Groups.Allsky);

    private readonly IProfileProvider _profile;
    private readonly DeviceFactory _deviceFactory;
    private readonly ExposureService _exposureService;
    private readonly SunService _sunService;
    private readonly IMemoryCache _memoryCache;

    public FindExposureJob(
        IProfileProvider profile,
        DeviceFactory deviceFactory,
        ExposureService exposureService,
        SunService sunService,
        IMemoryCache memoryCache)
    {
        _profile = profile;
        _deviceFactory = deviceFactory;
        _exposureService = exposureService;
        _sunService = sunService;
        _memoryCache = memoryCache;
    }

    protected async override Task OnExecute(IJobExecutionContext context)
    {
        bool success = _memoryCache.TryGetValue<TimeSpan>(CacheKeys.NextExposure, out var previousExposure);

        if (success)
        {
            Log.Information("Using last exposure of {Exposure:#.000000} sec",
                _exposureService.GetNextExposure().TotalSeconds);
        }
        else
        {
            IndiCamera camera = await _deviceFactory.GetOrCreateConnectedCamera(context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();
            
            bool isDay = _sunService.IsDaytime;
            var exposure = isDay
                ? TimeSpan.FromSeconds(1)
                : TimeSpan.FromSeconds(5);

            var gain = isDay
                ? _profile.Current.Camera.DaytimeGain
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

        context.CancellationToken.ThrowIfCancellationRequested();

        if (success)
        {
            var trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerKeys.ScheduledAllsky)
                .ForJob(CaptureJob.Key)
                .WithSimpleSchedule(o => o
                    .WithInterval(_profile.Current.Capture.CaptureInterval)
                    .WithMisfireHandlingInstructionIgnoreMisfires()
                    .RepeatForever())
                .Build();

            await context.Scheduler.ScheduleJob(trigger);
        }
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
