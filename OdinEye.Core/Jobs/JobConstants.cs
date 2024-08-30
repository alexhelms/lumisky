using Quartz;

namespace OdinEye.Core.Jobs;

public static class JobConstants
{
    public static class Jobs
    {
        public const string FindExposure = "find-exposure";
        public const string Capture = "capture";
        public const string Processing = "processing";
        public const string Export = "export";
    }

    public static class Triggers
    {
        public const string FindExposure = "find-exposure";
        public const string ScheduledAllsky = "scheduled-allsky";
    }

    public static class Groups
    {
        public const string Allsky = "allsky";
    }
}

public static class TriggerKeys
{
    public static readonly TriggerKey FindExposure = new(JobConstants.Triggers.FindExposure, JobConstants.Groups.Allsky);
    public static readonly TriggerKey ScheduledAllsky = new(JobConstants.Triggers.ScheduledAllsky, JobConstants.Groups.Allsky);
}