﻿using OdinEye.Core.Jobs;
using Quartz;
using Quartz.Impl.Matchers;

namespace OdinEye.Core.Services;

public class AllSkyScheduler
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
        if (IsRunning) return;

        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);
        
        Log.Information("AllSky service starting");
        
        var scheduler = await _schedulerFactory.GetScheduler();

        await scheduler.AddJob(JobBuilder.Create<FindExposureJob>()
            .WithIdentity(FindExposureJob.Key)
            .StoreDurably()
            .Build(), true);

        await scheduler.AddJob(JobBuilder.Create<CaptureJob>()
            .WithIdentity(CaptureJob.Key)
            .StoreDurably()
            .Build(), true);

        await scheduler.AddJob(JobBuilder.Create<ProcessingJob>()
            .WithIdentity(ProcessingJob.Key)
            .StoreDurably()
            .Build(), true);

        await scheduler.AddJob(JobBuilder.Create<ExportJob>()
            .WithIdentity(ExportJob.Key)
            .StoreDurably()
            .Build(), true);

        // Resume jobs since they may have been previously paused
        await scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(JobConstants.Groups.Allsky));

        // Triggering this job starts the pipeline.
        await scheduler.TriggerJob(FindExposureJob.Key);

        Log.Information("AllSky service started");
        IsRunning = true;
        AllSkyStarted?.Invoke(this, EventArgs.Empty);
    }

    public async Task Stop()
    {
        if (!IsRunning) return;

        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);
        
        Log.Information("AllSky service stopping");
        
        var scheduler = await _schedulerFactory.GetScheduler();

        IsStopping = true;
        AllSkyStopping?.Invoke(this, EventArgs.Empty);

        // Pause jobs so they won't run again
        await scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(JobConstants.Groups.Allsky));

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        // Cancel any ongoing jobs
        var executingJobs = await scheduler.GetCurrentlyExecutingJobs();
        if (executingJobs.Count > 0)
        {
            var allSkyJobs = executingJobs.Where(j => j.JobDetail.Key.Group == JobConstants.Groups.Allsky).ToList();
            if (allSkyJobs.Count > 0)
            {
                foreach (var job in allSkyJobs)
                {
                    await scheduler.Interrupt(job.FireInstanceId);
                }
            }

            // Wait for them to complete.
            do
            {
                executingJobs = await scheduler.GetCurrentlyExecutingJobs();
                allSkyJobs = executingJobs.Where(j => j.JobDetail.Key.Group == JobConstants.Groups.Allsky).ToList();
                await Task.Delay(50);
            } while (allSkyJobs.Count > 0 && !timeout.IsCancellationRequested);
        }

        if (timeout.IsCancellationRequested)
        {
            Log.Warning("Timed out waiting for AllSky jobs to complete");
        }

        await scheduler.DeleteJobs([
            FindExposureJob.Key,
            CaptureJob.Key,
            ProcessingJob.Key,
            ExportJob.Key,
        ]);

        Log.Information("AllSky service stopped");
        IsStopping = false;
        IsRunning = false;
        AllSkyStopped?.Invoke(this, EventArgs.Empty);
    }
}
