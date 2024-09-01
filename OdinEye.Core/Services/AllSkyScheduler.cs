using OdinEye.Core.Jobs;
using Quartz;
using Quartz.Listener;

namespace OdinEye.Core.Services;

public class AllSkyScheduler : SchedulerListenerSupport
{
    private readonly ISchedulerFactory _schedulerFactory;

    public event EventHandler? AllSkyStarted;
    public event EventHandler? AllSkyStopping;
    public event EventHandler? AllSkyStopped;

    public bool IsRunning { get; private set; }
    public bool IsStopping { get; private set; }

    public AllSkyScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task Start()
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);
        var scheduler = await _schedulerFactory.GetScheduler();

        if (scheduler.IsStarted) return;

        Log.Information("AllSky service starting");

        await scheduler.Start();

        // Jobs

        await scheduler.AddJob(JobBuilder
            .Create<FindExposureJob>()
            .WithIdentity(FindExposureJob.Key)
            .StoreDurably()
            .Build(), true);

        await scheduler.AddJob(JobBuilder
            .Create<CaptureJob>()
            .WithIdentity(CaptureJob.Key)
            .StoreDurably()
            .Build(), true);

        await scheduler.AddJob(JobBuilder
            .Create<ProcessingJob>()
            .WithIdentity(ProcessingJob.Key)
            .StoreDurably()
            .Build(), true);

        await scheduler.AddJob(JobBuilder
            .Create<ExportJob>()
            .WithIdentity(ExportJob.Key)
            .StoreDurably()
            .Build(), true);

        // Triggers

        var findExposureTrigger = TriggerBuilder.Create()
            .WithIdentity(TriggerKeys.FindExposure)
            .ForJob(FindExposureJob.Key)
            .StartNow()
            .Build();

        // Start the pipeline

        await scheduler.ScheduleJob(findExposureTrigger);

        Log.Information("AllSky service started");
        IsRunning = true;
        AllSkyStarted?.Invoke(this, EventArgs.Empty);
    }

    public async Task Stop()
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);
        var scheduler = await _schedulerFactory.GetScheduler();

        if (scheduler.IsShutdown) return;

        Log.Information("AllSky service stopping");

        IsStopping = true;
        AllSkyStopping?.Invoke(this, EventArgs.Empty);

        await scheduler.Shutdown(waitForJobsToComplete: true);

        Log.Information("AllSky service stopped");
        IsStopping = false;
        IsRunning = false;
        AllSkyStopped?.Invoke(this, EventArgs.Empty);
    }
}
