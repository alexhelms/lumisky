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

    [ObservableProperty]
    public partial bool AutoStart { get; set; }

    [ObservableProperty]
    public partial string DataDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TimeSpan CaptureInterval { get; set; }

    [ObservableProperty]
    public partial TimeSpan MaxExposureDuration { get; set; }
}
