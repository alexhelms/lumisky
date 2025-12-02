using Humanizer;
using LumiSky.Core.IO;
using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Quartz;

namespace LumiSky.Core.Jobs;

[DisallowConcurrentExecution]
public class DiskSpaceJob(IProfileProvider profile, NotificationService notificationService) : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.DiskSpace, JobConstants.Groups.Maintenance);

    protected async override Task OnExecute(IJobExecutionContext context)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);

        await CheckDiskSpace(LumiSkyPaths.BasePath);

        // Only check the image path if it is somewhere different 
        if (!profile.Current.App.ImageDataPath.StartsWith(LumiSkyPaths.BasePath))
        {
            await CheckDiskSpace(profile.Current.App.ImageDataPath);
        }
    }

    private async Task CheckDiskSpace(string path)
    {
        try
        {
            const int CriticallyLowThreshold = 1;
            const int VeryLowThreshold = 5;
            const int LowThreshold = 10;

            var driveInfo = new DriveInfo(path);
            var availableGB = driveInfo.AvailableFreeSpace / (double)(1024 * 1024 * 1024);
            string message = availableGB switch
            {
                < CriticallyLowThreshold => "critically low",
                < VeryLowThreshold => "very low",
                < LowThreshold => "low",
                _ => string.Empty,
            };

            if (!string.IsNullOrEmpty(message))
            {
                Log.Warning(
                    "Available disk space is {Message} ({AvailableGB:F3} GB) at {Path}",
                    message, availableGB, path);

                var notification = new NotificationMessage
                {
                    Type = availableGB switch
                    {
                        < CriticallyLowThreshold => NotificationType.Error,
                        < VeryLowThreshold => NotificationType.Warning,
                        < LowThreshold => NotificationType.Warning,
                        _ => NotificationType.Info,
                    },
                    Summary = $"{message.Titleize()} Disk Space",
                    Detail = $"{availableGB:F3} GB available at {path}",
                };

                await notificationService.SendNotification(notification);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting available disk space for {Path}", path);
        }
    }
}
