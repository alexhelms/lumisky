using OdinEye.Core.DomainEvents;
using OdinEye.Core.Services;
using Quartz;
using SlimMessageBus;

namespace OdinEye.Core.Jobs;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class DayNightJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.DayNightJob, JobConstants.Groups.Maintenance);
    public static readonly string SkyStateKey = "SkyState";

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
        var currentState = _sunService.IsDaytime ? SkyState.Day : SkyState.Night;
        if (data.TryGetInt(SkyStateKey, out var value))
        {
            prevState = (SkyState)value;
        }
        else
        {
            prevState = currentState;
        }

        if (currentState != prevState)
        {
            Log.Information("Changing from {PreviousState} to {CurrentState}", prevState, currentState);

            if (currentState == SkyState.Day)
            {
                await _messageBus.Publish(new BecomingDaytimeEvent()).ConfigureAwait(false);
            }
            else if (currentState == SkyState.Night)
            {
                await _messageBus.Publish(new BecomingNighttimeEvent()).ConfigureAwait(false);
            }
        }

        data.Put(SkyStateKey, (int)currentState);
    }
}