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
            using var camera = await _deviceFactory.GetCamera(context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();
            
            bool isDay = _sunService.IsDaytime();
            var exposure = isDay
                ? TimeSpan.FromSeconds(1)
                : TimeSpan.FromSeconds(5);

            var gain = isDay
                ? _profile.Current.Camera.DaytimeGain
                : _profile.Current.Camera.NighttimeGain;

            var bias = isDay
                ? _profile.Current.Camera.DaytimeBiasG
                : _profile.Current.Camera.NighttimeBiasG;

            var lowerThresh = bias * 1.1;
            var upperThresh = 0.9;

            var exposureParameters = new ExposureParameters
            {
                Duration = exposure,
                Gain = gain,
                Offset = _profile.Current.Camera.Offset,
                Binning = _profile.Current.Camera.Binning,
            };

            Log.Information("Finding initial exposure");

            int iters = 0;
            while (true)
            {
                iters++;

                context.CancellationToken.ThrowIfCancellationRequested();

                var median = await ExposeAndMeasureMedian(camera, exposureParameters, context.CancellationToken);
                _exposureService.AddMostRecentStatistics(exposureParameters.Duration, median, gain);

                var nextExposure = _exposureService.GetNextExposure();
                exposureParameters = exposureParameters with { Duration = nextExposure };

                if (median > lowerThresh && median < upperThresh)
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

                if (iters >= 10)
                {
                    string message = "Failed to find starting exposure, check gain and try again.";
                    Log.Error(message);
                    throw new Exception("Failed to find starting exposure, check gain and try again.");
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
        ICamera camera,
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
