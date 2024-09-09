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
        var (lat, lon) = GetLatLon();
        var now = DateTime.Now;
        var yesterdayPositions = _sunService.GetSunTimes(DateOnly.FromDateTime(now.AddDays(-1)), lat, lon);
        var todayPositions = _sunService.GetSunTimes(DateOnly.FromDateTime(now), lat, lon);

        // This shouldn't happen because the event shouldn't happen if it is not becoming daytime.
        // Log this improbable event.
        if (!yesterdayPositions.Dusk.HasValue)
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

        var yesterdayDuskUtc = yesterdayPositions.Dusk.Value.ToUniversalTime();
        var todayDawnUtc = todayPositions.Dawn.Value.ToUniversalTime();

        if (_profile.Current.Generation.EnableNighttimeTimelapse)
        {
            Log.Information("Creating nighttime timelapse from {YesterdayDusk:G} to {TodayDawn:G}",
                yesterdayDuskUtc.ToLocalTime(), todayDawnUtc.ToLocalTime());
            await _generationService.GenerateTimelapse(yesterdayDuskUtc, todayDawnUtc);
        }

        if (_profile.Current.Generation.EnableNighttimePanorama)
        {
            Log.Information("Creating nighttime panorama timelapse from {YesterdayDusk:G} to {TodayDawn:G}",
                yesterdayDuskUtc.ToLocalTime(), todayDawnUtc.ToLocalTime());
            await _generationService.GeneratePanoramaTimelapse(yesterdayDuskUtc, todayDawnUtc);
        }
    }

    public async Task OnHandle(BecomingNighttimeEvent message)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName);
        var (lat, lon) = GetLatLon();
        var now = DateTime.Now;
        var todayPositions = _sunService.GetSunTimes(DateOnly.FromDateTime(now), lat, lon);

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

        var todayDawnUtc = todayPositions.Dawn.Value.ToUniversalTime();
        var todayDuskUtc = todayPositions.Dusk.Value.ToUniversalTime();

        if (_profile.Current.Generation.EnableDaytimeTimelapse)
        {
            Log.Information("Creating daytime timelapse from {TodayDawn:G} to {TodayDusk:G}",
                todayDawnUtc.ToLocalTime(), todayDuskUtc.ToLocalTime());
            await _generationService.GenerateTimelapse(todayDawnUtc, todayDuskUtc);
        }

        if (_profile.Current.Generation.EnableDaytimePanorama)
        {
            Log.Information("Creating daytime panorama timelapse from {TodayDawn:G} to {TodayDusk:G}",
                todayDawnUtc.ToLocalTime(), todayDuskUtc.ToLocalTime());
            await _generationService.GeneratePanoramaTimelapse(todayDawnUtc, todayDuskUtc);
        }
    }

    private (double, double) GetLatLon()
    {
        var latitude = _profile.Current.Location.Latitude;
        var longitude = _profile.Current.Location.Longitude;
        return (latitude, longitude);
    }
}
