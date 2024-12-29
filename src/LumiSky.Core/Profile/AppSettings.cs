using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.IO;
using LumiSky.Core.Utilities;

namespace LumiSky.Core.Profile;

public interface IAppSettings : ISettings
{
    string AppVersion { get; set; }
    string ImageDataPath { get; set; }
    bool EnableCleanup { get; set; }
    bool EnableImageCleanup { get; set; }
    bool EnableRawImageCleanup { get; set; }
    bool EnableTimelapseCleanup { get; set; }
    bool EnablePanoramaCleanup { get; set; }
    bool EnablePanoramaTimelapseCleanup { get; set; }
    int ImageCleanupAge { get; set; }
    int RawImageCleanupAge { get; set; }
    int TimelapseCleanupAge { get; set; }
    int PanoramaCleanupAge { get; set; }
    int PanoramaTimelapseCleanupAge { get; set; }
}

public sealed partial class AppSettings : Settings, IAppSettings
{
    protected override void Reset()
    {
        AppVersion = RuntimeUtil.Version;
        ImageDataPath = Path.Combine(LumiSkyPaths.BasePath, "data");
        EnableCleanup = true;
        EnableImageCleanup = true;
        EnableRawImageCleanup = true;
        EnableTimelapseCleanup = true;
        EnablePanoramaCleanup = true;
        EnablePanoramaTimelapseCleanup = true;
        ImageCleanupAge = 30;
        RawImageCleanupAge = 30;
        TimelapseCleanupAge = 30;
        PanoramaCleanupAge = 30;
        PanoramaTimelapseCleanupAge = 30;
    }

    [ObservableProperty]
    public partial string AppVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageDataPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EnableCleanup { get; set; }

    [ObservableProperty]
    public partial bool EnableImageCleanup { get; set; }

    [ObservableProperty]
    public partial bool EnableRawImageCleanup { get; set; }

    [ObservableProperty]
    public partial bool EnableTimelapseCleanup { get; set; }

    [ObservableProperty]
    public partial bool EnablePanoramaCleanup { get; set; }

    [ObservableProperty]
    public partial bool EnablePanoramaTimelapseCleanup { get; set; }

    [ObservableProperty]
    public partial int ImageCleanupAge { get; set; }

    [ObservableProperty]
    public partial int RawImageCleanupAge { get; set; }

    [ObservableProperty]
    public partial int TimelapseCleanupAge { get; set; }

    [ObservableProperty]
    public partial int PanoramaCleanupAge { get; set; }

    [ObservableProperty]
    public partial int PanoramaTimelapseCleanupAge { get; set; }
}
