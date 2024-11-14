using LumiSky.Core.DomainEvents;
using LumiSky.Core.Mathematics;
using LumiSky.Core.Services;
using Quartz;
using SlimMessageBus;

namespace LumiSky.Core.Jobs;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class DayNightJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.DayNightJob, JobConstants.Groups.Maintenance);
    public static readonly string SkyStateKey = "SkyState";
    public static readonly string TransitionKey = "Transition";

    private readonly IMessageBus _messageBus;
    private readonly SunService _sunService;

    private enum SkyState
    {
        Day,
        Night,
    }

    public DayNightJob(
        IMessageBus messageBus,
        SunService sunService)
    {
        _messageBus = messageBus;
        _sunService = sunService;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);

        var data = context.JobDetail.JobDataMap;

        SkyState prevState;
        DateTime prevTransition;
        var currentState = _sunService.IsDaytime ? SkyState.Day : SkyState.Night;
        var utcNow = DateTime.UtcNow;

        if (data.TryGetInt(SkyStateKey, out var value))
        {
            prevState = (SkyState)value;
        }
        else
        {
            prevState = currentState;
        }

        if (!data.TryGetDateTime(TransitionKey, out prevTransition))
        {
            prevTransition = utcNow;
        }

        if (currentState != prevState)
        {
            Log.Information("Changing from {PreviousState} to {CurrentState}", prevState, currentState);

            // Force a transition if it has been at least 1 sidereal day.
            // If the user is high latitude and doesn't have a real day/night transition,
            // they will experience another day/night as appropriate. For example, if
            // the user is in northern Alaska in June and has no night, after 1 sidereal day
            // the day event message will be published again. Later in the year when night
            // returns, the transition will occur normally depending on sun angle.
            bool forceTransition = utcNow - prevTransition >= TimeSpan.FromHours(LumiSkyMath.SiderealDayInHours);

            if (currentState == SkyState.Day || forceTransition)
            {
                prevTransition = utcNow;
                await _messageBus.Publish(new NightToDayEvent()).ConfigureAwait(false);
            }
            else if (currentState == SkyState.Night || forceTransition)
            {
                prevTransition = utcNow;
                await _messageBus.Publish(new DayToNightEvent()).ConfigureAwait(false);
            }
        }

        data.Put(SkyStateKey, (int)currentState);
        data.Put(TransitionKey, prevTransition);
    }
}