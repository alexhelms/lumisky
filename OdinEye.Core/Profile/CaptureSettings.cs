using CommunityToolkit.Mvvm.ComponentModel;
using OdinEye.Core.IO;

namespace OdinEye.Core.Profile;

public interface ICaptureSettings : ISettings
{
    bool AutoStart { get; set; }
    string DataDirectory { get; set; }
    TimeSpan CaptureInterval { get; set; }
    double DayNightTransitionAltitude { get; set; }
}

public sealed partial class CaptureSettings : Settings, ICaptureSettings
{
    protected override void Reset()
    {
        AutoStart = false;
        DataDirectory = Path.Combine(OdinEyePaths.BasePath, "data");
        CaptureInterval = TimeSpan.FromMinutes(1);
        DayNightTransitionAltitude = -6;
    }

    [ObservableProperty] bool _autoStart;
    [ObservableProperty] string _dataDirectory = string.Empty;
    [ObservableProperty] TimeSpan _captureInterval;
    [ObservableProperty] double _DayNightTransitionAltitude;
}
