using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Quartz;

namespace LumiSky.Core.Jobs;

public class PublishJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Publish, JobConstants.Groups.Allsky);

    private readonly IProfileProvider _profile;
    private readonly PublishService _publishService;

    public string? ImageFilename { get; set; }
    public string? PanoramaFilename { get; set; }
    public string? DayTimelapseFilename { get; set; }
    public string? NightTimelapseFilename { get; set; }

    public PublishJob(
        IProfileProvider profileProvider,
        PublishService publishService)
    {
        _profile = profileProvider;
        _publishService = publishService;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        if (!_profile.Current.Publish.EnablePublish) return;

        List<Func<Task>> taskFuncs = [];

        if (_profile.Current.Publish.PublishImage &&
            ImageFilename is not null)
        {
            taskFuncs.Add(() => _publishService.Upload(ImageFilename, "latest_image", context.CancellationToken));
        }

        if (_profile.Current.Publish.PublishPanorama &&
            PanoramaFilename is not null)
        {
            taskFuncs.Add(() => _publishService.Upload(PanoramaFilename, "latest_panorama", context.CancellationToken));
        }

        if (_profile.Current.Publish.PublishNightTimelapse &&
            NightTimelapseFilename is not null)
        {
            taskFuncs.Add(() => _publishService.Upload(NightTimelapseFilename, "latest_night_timelapse", context.CancellationToken));
        }

        if (_profile.Current.Publish.PublishDayTimelapse &&
            DayTimelapseFilename is not null)
        {
            taskFuncs.Add(() => _publishService.Upload(DayTimelapseFilename, "latest_day_timelapse", context.CancellationToken));
        }

        // Write the metadata first so the webpage displays the correct info
        await _publishService.SetMetadata(context.CancellationToken);
        
        await Task.WhenAll(taskFuncs.Select(x => x()));
    }
}
