using LumiSky.Core.Jobs;
using Quartz;
using Quartz.Listener;

namespace LumiSky.Core.Services;

public class JobExceptionListener : JobListenerSupport
{
    private readonly AllSkyScheduler _allSkyScheduler;
    private readonly NotificationService _notificationService;

    public override string Name => "Job Exception Listener";

    public JobExceptionListener(
        AllSkyScheduler allSkyScheduler,
        NotificationService notificationService)
    {
        _allSkyScheduler = allSkyScheduler;
        _notificationService = notificationService;
    }

    public override async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        if (jobException is not null)
        {
            var jobName = context.JobInstance.GetType().Name;

            await _notificationService.SendNotification(new NotificationMessage
            {
                Type = NotificationType.Error,
                Summary = "Allsky Error",
                Detail = jobException.Message,
            });

            // It is critical that this job succeed as it is the start of the pipeline.
            // If FindExposureJob fails, abort the pipeline.
            if (context.JobDetail.Key == FindExposureJob.Key)
            {
                // Do not block or deadlock :)
                _ = Task.Run(() => _allSkyScheduler.Stop());

                await _notificationService.SendNotification(new NotificationMessage
                {
                    Type = NotificationType.Warning,
                    Summary = "Allsky Status",
                    Detail = "Allsky stopped!",
                });
            }
        }
    }
}