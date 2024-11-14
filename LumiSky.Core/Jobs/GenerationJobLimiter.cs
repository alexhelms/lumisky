using Quartz;
using Quartz.Listener;

namespace LumiSky.Core.Jobs;

/// <summary>
/// Limits concurrency of jobs in the generation group.
/// These jobs are very CPU intensive and should only be run individually.
/// </summary>
public class GenerationJobLimiter : TriggerListenerSupport
{
    public override string Name => "Generation Job Limiter";

    public override async Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (trigger.Key.Group == JobConstants.Groups.Generation)
        {
            // Get the currently executing jobs
            var executingJobs = await context.Scheduler
                .GetCurrentlyExecutingJobs()
                .ConfigureAwait(false);

            // Is there another generation job running?
            var anyGenerationJobRunning = executingJobs
                .Where(j => j.JobDetail.Key.Group == JobConstants.Groups.Generation)
                .Any();

            if (anyGenerationJobRunning)
            {
                // Reschedule the job a few seconds from now
                var newTrigger = trigger.GetTriggerBuilder()
                    .StartAt(DateTimeOffset.UtcNow.AddSeconds(3))
                    .Build();

                // Reschedule
                await context.Scheduler
                    .RescheduleJob(trigger.Key, newTrigger, cancellationToken)
                    .ConfigureAwait(false);

                // Veto
                return true;
            }
        }

        return false;
    }
}
