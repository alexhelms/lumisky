using Quartz;

namespace OdinEye.Core.Jobs;

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
    }

    public static class Triggers
    {
        public const string ScheduledAllsky = "scheduled-allsky-trigger";
    }

    public static class Groups
    {
        public const string Allsky = "allsky";
        public const string Generation = "generation";
    }
}

public static class TriggerKeys
{
    public static readonly TriggerKey ScheduledAllsky = new(JobConstants.Triggers.ScheduledAllsky, JobConstants.Groups.Allsky);
}