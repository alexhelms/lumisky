using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.Video;

namespace LumiSky.Core.Profile;

public interface IGenerationSettings : ISettings
{
    string FfmpegPath { get; set; }
    string FfprobePath { get; set; }
    bool EnableDaytimeTimelapse { get; set; }
    bool EnableNighttimeTimelapse { get; set; }
    int TimelapseFrameRate { get; set; }
    int TimelapseQuality { get; set; }
    int TimelapseWidth { get; set; }
    VideoCodec TimelapseCodec { get; set; }
    bool EnableDaytimePanorama { get; set; }
    bool EnableNighttimePanorama { get; set; }
    int PanoramaFrameRate { get; set; }
    int PanoramaQuality { get; set; }
    int PanoramaWidth { get; set; }
    VideoCodec PanoramaCodec { get; set; }
}

public sealed partial class GenerationSettings : Settings, IGenerationSettings
{
    protected override void Reset()
    {
        FfmpegPath = Ffmpeg.DefaultFfmpegPath;
        FfprobePath = Ffprobe.DefaultFfprobePath;

        EnableDaytimeTimelapse = true;
        EnableNighttimeTimelapse = true;
        TimelapseFrameRate = 30;
        TimelapseQuality = 25;
        TimelapseWidth = 0;
        TimelapseCodec = VideoCodec.H264;
        EnableDaytimePanorama = false;
        EnableNighttimePanorama = false;
        PanoramaFrameRate = 30;
        PanoramaQuality = 25;
        PanoramaWidth = 0;
        PanoramaCodec = VideoCodec.H264;
    }

    [ObservableProperty]
    public partial string FfmpegPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FfprobePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EnableDaytimeTimelapse { get; set; }

    [ObservableProperty]
    public partial bool EnableNighttimeTimelapse { get; set; }

    [ObservableProperty]
    public partial int TimelapseFrameRate { get; set; }

    [ObservableProperty]
    public partial int TimelapseQuality { get; set; }

    [ObservableProperty]
    public partial int TimelapseWidth { get; set; }

    [ObservableProperty]
    public partial VideoCodec TimelapseCodec { get; set; }

    [ObservableProperty]
    public partial bool EnableDaytimePanorama { get; set; }

    [ObservableProperty]
    public partial bool EnableNighttimePanorama { get; set; }

    [ObservableProperty]
    public partial int PanoramaFrameRate { get; set; }

    [ObservableProperty]
    public partial int PanoramaQuality { get; set; }

    [ObservableProperty]
    public partial int PanoramaWidth { get; set; }

    [ObservableProperty]
    public partial VideoCodec PanoramaCodec { get; set; }
}

public enum VideoCodec
{
    H264,
    H265,
}