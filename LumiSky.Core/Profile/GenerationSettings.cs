using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices;

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
        FfmpegPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\ffmpeg\ffmpeg.exe"
            : "/usr/bin/ffmpeg";

        FfprobePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\ffmpeg\ffprobe.exe"
            : "/usr/bin/ffprobe";

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

    [ObservableProperty] string _ffmpegPath = string.Empty;
    [ObservableProperty] string _ffprobePath = string.Empty;
    [ObservableProperty] bool _enableDaytimeTimelapse;
    [ObservableProperty] bool _enableNighttimeTimelapse;
    [ObservableProperty] int _timelapseFrameRate;
    [ObservableProperty] int _timelapseQuality;
    [ObservableProperty] int _timelapseWidth;
    [ObservableProperty] VideoCodec _timelapseCodec;
    [ObservableProperty] bool _enableDaytimePanorama;
    [ObservableProperty] bool _enableNighttimePanorama;
    [ObservableProperty] int _panoramaFrameRate;
    [ObservableProperty] int _panoramaQuality;
    [ObservableProperty] int _panoramaWidth;
    [ObservableProperty] VideoCodec _panoramaCodec;
}

public enum VideoCodec
{
    H264,
    H265,
}