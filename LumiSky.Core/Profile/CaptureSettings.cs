using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.IO;

namespace LumiSky.Core.Profile;

public interface ICaptureSettings : ISettings
{
    bool AutoStart { get; set; }
    string DataDirectory { get; set; }
    TimeSpan CaptureInterval { get; set; }
    TimeSpan MaxExposureDuration { get; set; }
}

public sealed partial class CaptureSettings : Settings, ICaptureSettings
{
    protected override void Reset()
    {
        AutoStart = false;
        DataDirectory = Path.Combine(LumiSkyPaths.BasePath, "data");
        CaptureInterval = TimeSpan.FromMinutes(1);
        MaxExposureDuration = TimeSpan.FromSeconds(50);
    }

    [ObservableProperty] bool _autoStart;
    [ObservableProperty] string _dataDirectory = string.Empty;
    [ObservableProperty] TimeSpan _captureInterval;
    [ObservableProperty] TimeSpan _maxExposureDuration;
}
