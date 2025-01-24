using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Quartz;

namespace LumiSky.Core.Jobs;

public class ExportJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Export, JobConstants.Groups.Allsky);
    
    private readonly IProfileProvider _profile;
    private readonly FtpService _ftpService;
    private readonly NotificationService _notificationService;

    public ExportJob(
        IProfileProvider profile,
        FtpService ftpService,
        NotificationService notificationService)
    {
        _profile = profile;
        _ftpService = ftpService;
        _notificationService = notificationService;
    }

    public string? RawFilename { get; set; }
    public string? ImageFilename { get; set; }
    public string? PanoramaFilename { get; set; }
    public string? TimelapseFilename { get; set; }
    public string? PanoramaTimelapseFilename { get; set; }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        if (!_profile.Current.Export.EnableExport) return;

        var localFilenames = new List<string>();

        if (_profile.Current.Export.ExportRaws &&
            RawFilename is not null &&
            File.Exists(RawFilename))
        {
            localFilenames.Add(RawFilename);
        }

        if (_profile.Current.Export.ExportImages &&
            ImageFilename is not null &&
            File.Exists(ImageFilename))
        {
            localFilenames.Add(ImageFilename);
        }

        if (_profile.Current.Export.ExportPanoramas &&
            PanoramaFilename is not null &&
            File.Exists(PanoramaFilename))
        {
            localFilenames.Add(PanoramaFilename);
        }

        if (_profile.Current.Export.ExportTimelapses &&
            TimelapseFilename is not null &&
            File.Exists(TimelapseFilename))
        {
            localFilenames.Add(TimelapseFilename);
        }

        if (_profile.Current.Export.ExportPanoramaTimelapses &&
            PanoramaTimelapseFilename is not null &&
            File.Exists(PanoramaTimelapseFilename))
        {
            localFilenames.Add(PanoramaTimelapseFilename);
        }

        if (localFilenames.Count > 0)
        {
            try
            {
                await _ftpService.UploadFiles(localFilenames, context.CancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                // Don't notify of errors if the error was caused by a cancellation.
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                Log.Warning(e, "Error exporting via FTP: {Message}", e.Message);
                await _notificationService.SendNotification(new NotificationMessage
                {
                    Type = NotificationType.Warning,
                    Summary = "Error Exporting via FTP",
                    Detail = e.Message,
                });
            }
        }
    }
}
