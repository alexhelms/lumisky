using OdinEye.Core.DomainEvents;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using SlimMessageBus;

namespace OdinEye.Core.Handlers;

public class DayNightHandler :
    IConsumer<BecomingDaytimeEvent>,
    IConsumer<BecomingNighttimeEvent>
{
    private static readonly string SourceContextName = "DayNightHandler";

    private readonly IProfileProvider _profile;
    private readonly SunService _sunService;
    private readonly GenerationService _generationService;

    public DayNightHandler(
        IProfileProvider profile,
        SunService sunService,
        GenerationService generationService)
    {
        _profile = profile;
        _sunService = sunService;
        _generationService = generationService;
    }

    public async Task OnHandle(BecomingDaytimeEvent message)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName);
        var todayPositions = _sunService.GetSunTimes(DateOnly.FromDateTime(DateTime.Now));

        // This shouldn't happen because the event shouldn't happen if it is not becoming daytime.
        // Log this improbable event.
        if (!todayPositions.Dusk.HasValue)
        {
            Log.Warning("Location does not have dusk.");
            return;
        }

        // This shouldn't happen because the event shouldn't happen if it is not becoming daytime.
        // Log this improbable event.
        if (!todayPositions.Dawn.HasValue)
        {
            Log.Warning("Location does not have dawn.");
            return;
        }

        // Note: ignoring the minor time error by using todays dusk instead of yesterday.
        var duskUtc = todayPositions.Dusk.Value;
        var dawnUtc = todayPositions.Dawn.Value;
        var duskLocal = duskUtc.ToLocalTime();
        var dawnLocal = dawnUtc.ToLocalTime();

        if (_profile.Current.Generation.EnableNighttimeTimelapse)
        {
            Log.Information("Creating nighttime timelapse from {YesterdayDusk:G} to {TodayDawn:G}", duskLocal, dawnLocal);
            await _generationService.GenerateTimelapse(duskUtc, dawnUtc);
        }

        if (_profile.Current.Generation.EnableNighttimePanorama)
        {
            Log.Information("Creating nighttime panorama timelapse from {YesterdayDusk:G} to {TodayDawn:G}", duskLocal, dawnLocal);
            await _generationService.GeneratePanoramaTimelapse(duskUtc, dawnUtc);
        }
    }

    public async Task OnHandle(BecomingNighttimeEvent message)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName);
        var todayPositions = _sunService.GetSunTimes(DateOnly.FromDateTime(DateTime.Now));

        // This shouldn't happen because the event shouldn't happen if it is not becoming nighttime.
        // Log this improbable event.
        if (!todayPositions.Dawn.HasValue)
        {
            Log.Warning("Location does not have dawn.");
            return;
        }

        // This shouldn't happen because the event shouldn't happen if it is not becoming nighttime.
        // Log this improbable event.
        if (!todayPositions.Dusk.HasValue)
        {
            Log.Warning("Location does not have dusk.");
            return;
        }

        var duskUtc = todayPositions.Dusk.Value;
        var dawnUtc = todayPositions.Dawn.Value;
        var duskLocal = duskUtc.ToLocalTime();
        var dawnLocal = dawnUtc.ToLocalTime();

        if (_profile.Current.Generation.EnableDaytimeTimelapse)
        {
            Log.Information("Creating daytime timelapse from {TodayDawn:G} to {TodayDusk:G}", dawnLocal, duskLocal);
            await _generationService.GenerateTimelapse(dawnUtc, duskUtc);
        }

        if (_profile.Current.Generation.EnableDaytimePanorama)
        {
            Log.Information("Creating daytime panorama timelapse from {TodayDawn:G} to {TodayDusk:G}", dawnLocal, duskLocal);
            await _generationService.GeneratePanoramaTimelapse(dawnUtc, duskUtc);
        }
    }
}
