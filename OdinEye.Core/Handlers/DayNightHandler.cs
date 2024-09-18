using OdinEye.Core.DomainEvents;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using SlimMessageBus;

namespace OdinEye.Core.Handlers;

public class DayNightHandler :
    IConsumer<NightToDayEvent>,
    IConsumer<DayToNightEvent>
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

    public async Task OnHandle(NightToDayEvent message)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName);

        var utcNow = DateTime.UtcNow;
        var utcNowDate = DateOnly.FromDateTime(utcNow);

        DateTime nightBegin;
        DateTime nightEnd;

        var sunTime = _sunService.GetRiseSetTime(utcNow, _profile.Current.Location.TransitionSunAltitude);
        if (sunTime is not null)
        {
            // Today's set time minus 1 day is close enough
            nightBegin = sunTime.Set.AddDays(-1);
            nightEnd = utcNow;
        }
        else
        {
            // Worst case, the beginning is 1 day ago.
            nightBegin = utcNow.AddDays(-1);
            nightEnd = utcNow;
        }

        if (_profile.Current.Generation.EnableNighttimeTimelapse)
        {
            Log.Information("Creating nighttime timelapse from {NightBegin:G} to {NightEnd:G}",
                nightBegin.ToLocalTime(), nightEnd.ToLocalTime());
            await _generationService.GenerateTimelapse(nightBegin, nightEnd);
        }

        if (_profile.Current.Generation.EnableNighttimePanorama)
        {
            Log.Information("Creating nighttime panorama timelapse from {NightBegin:G} to {NightEnd:G}",
                nightBegin.ToLocalTime(), nightEnd.ToLocalTime());
            await _generationService.GeneratePanoramaTimelapse(nightBegin, nightEnd);
        }
    }

    public async Task OnHandle(DayToNightEvent message)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName);

        var utcNow = DateTime.UtcNow;
        var utcNowDate = DateOnly.FromDateTime(utcNow);

        DateTime dayBegin;
        DateTime dayEnd;

        var sunTime = _sunService.GetRiseSetTime(utcNow, _profile.Current.Location.TransitionSunAltitude);
        if (sunTime is not null)
        {
            // Today's rise time is close enough
            dayBegin = sunTime.Rise;
            dayEnd = utcNow;
        }
        else
        {
            // Worst case, the beginning is 1 day ago.
            dayBegin = utcNow.AddDays(-1);
            dayEnd = utcNow;
        }

        if (_profile.Current.Generation.EnableDaytimeTimelapse)
        {
            Log.Information("Creating daytime timelapse from {DayBegin:G} to {DayEnd:G}",
                dayBegin.ToLocalTime(), dayEnd.ToLocalTime());
            await _generationService.GenerateTimelapse(dayBegin, dayEnd);
        }

        if (_profile.Current.Generation.EnableDaytimePanorama)
        {
            Log.Information("Creating daytime panorama timelapse from {DayBegin:G} to {DayEnd:G}",
                dayBegin.ToLocalTime(), dayEnd.ToLocalTime());
            await _generationService.GeneratePanoramaTimelapse(dayBegin, dayEnd);
        }
    }
}
