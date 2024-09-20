using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OdinEye.Core.Data;
using OdinEye.Core.DomainEvents;
using OdinEye.Core.Jobs;
using Quartz;
using SlimMessageBus;

namespace OdinEye.Core.Services;

public class GenerationService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMessageBus _messageBus;

    public event EventHandler<GenerationQueued>? Queued;
    public event EventHandler<GenerationStarting>? Starting;
    public event EventHandler<GenerationProgress>? Progress;
    public event EventHandler<GenerationComplete>? Complete;

    public GenerationService(
        ISchedulerFactory schedulerFactory,
        IServiceScopeFactory serviceScopeFactory,
        IMessageBus messageBus)
    {
        _schedulerFactory = schedulerFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _messageBus = messageBus;
    }

    public void OnQueued(GenerationQueued message)
    {
        Queued?.Invoke(this, message);
    }

    public void OnStarting(GenerationStarting message)
    {
        Starting?.Invoke(this, message);
    }

    public void OnProgress(GenerationProgress message)
    {
        Progress?.Invoke(this, message);
    }

    public void OnComplete(GenerationComplete message)
    {
        Complete?.Invoke(this, message);
    }

    public async Task GenerateTimelapse(DateTime beginUtc, DateTime endUtc)
    {
        var generation = new Generation
        {
            State = GenerationState.Queued,
            Kind = GenerationKind.Timelapse,
            RangeBegin = new DateTimeOffset(beginUtc).ToUnixTimeSeconds(),
            RangeEnd = new DateTimeOffset(endUtc).ToUnixTimeSeconds(),
        };

        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Generations.Add(generation);
        await dbContext.SaveChangesAsync();

        if (generation.Id == 0)
            throw new InvalidOperationException("Failed to create generation entity");

        await _messageBus.Publish(new GenerationQueued { Id = generation.Id });

        var scheduler = await _schedulerFactory.GetScheduler();
        var jobData = new JobDataMap
        {
            [nameof(TimelapseJob.GenerationId)] = generation.Id,
        };

        var trigger = await scheduler.GetTrigger(TriggerKeys.Timelapse);
        if (trigger is null)
        {
            trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerKeys.Timelapse)
                .ForJob(TimelapseJob.Key)
                .UsingJobData(jobData)
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(trigger);
        }
        else
        {
            await scheduler.TriggerJob(TimelapseJob.Key, jobData);
        }
    }

    public async Task GeneratePanoramaTimelapse(DateTime beginUtc, DateTime endUtc)
    {
        var generation = new Generation
        {
            State = GenerationState.Queued,
            Kind = GenerationKind.PanoramaTimelapse,
            RangeBegin = new DateTimeOffset(beginUtc).ToUnixTimeSeconds(),
            RangeEnd = new DateTimeOffset(endUtc).ToUnixTimeSeconds(),
        };

        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Generations.Add(generation);
        await dbContext.SaveChangesAsync();

        if (generation.Id == 0)
            throw new InvalidOperationException("Failed to create generation entity");

        await _messageBus.Publish(new GenerationQueued { Id = generation.Id });

        var scheduler = await _schedulerFactory.GetScheduler();
        var jobData = new JobDataMap
        {
            [nameof(PanoramaTimelapseJob.GenerationId)] = generation.Id,
        };

        var trigger = await scheduler.GetTrigger(TriggerKeys.PanoramaTimelapse);
        if (trigger is null)
        {
            trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerKeys.PanoramaTimelapse)
                .ForJob(PanoramaTimelapseJob.Key)
                .UsingJobData(jobData)
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(trigger);
        }
        else
        {
            await scheduler.TriggerJob(PanoramaTimelapseJob.Key, jobData);
        }
    }

    public async Task CancelGeneration(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var generation = await dbContext.Generations
            .FirstOrDefaultAsync(x => x.Id == id);

        if (generation is not null)
        {
            if (!string.IsNullOrEmpty(generation.JobInstanceId))
            {
                // Interrupt the running job, the generation state will transition to Failed
                var scheduler = await _schedulerFactory.GetScheduler();
                await scheduler.Interrupt(generation.JobInstanceId);
            }
            else
            {
                // Cancel the queued job
                if (generation.State == GenerationState.Queued)
                {
                    generation.State = GenerationState.Canceled;
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }

    public async Task DeleteGeneration(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Generations
            .Where(g => g.Id == id)
            .ExecuteDeleteAsync();
    }
}
