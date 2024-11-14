using Quartz;

namespace LumiSky.Core.Jobs;

public static class JobConstants
{
    public static class Jobs
    {
        public const string FindExposure = "find-exposure-job";
        public const string Capture = "capture-job";
        public const string Processing = "processing-job";
        public const string Export = "export-job";
        public const string Timelapse = "timelapse-job";
        public const string PanoramaTimelapse = "panorama-timelapse-job";
        public const string DayNightJob = "day-night-job";
    }

    public static class Triggers
    {
        public const string ScheduledAllsky = "scheduled-allsky-trigger";
        public const string Timelapse = "timelapse-trigger";
        public const string PanoramaTimelapse = "panorama-timelapse-trigger";
        public const string DayNight = "day-night-trigger";
    }

    public static class Groups
    {
        public const string Allsky = "allsky";
        public const string Generation = "generation";
        public const string Maintenance = "maintenance";
    }
}

public static class TriggerKeys
{
    public static readonly TriggerKey ScheduledAllsky = new(JobConstants.Triggers.ScheduledAllsky, JobConstants.Groups.Allsky);
    public static readonly TriggerKey Timelapse = new(JobConstants.Triggers.Timelapse, JobConstants.Groups.Generation);
    public static readonly TriggerKey PanoramaTimelapse = new(JobConstants.Triggers.PanoramaTimelapse, JobConstants.Groups.Generation);
    public static readonly TriggerKey DayNight = new(JobConstants.Triggers.DayNight, JobConstants.Groups.Maintenance);
}