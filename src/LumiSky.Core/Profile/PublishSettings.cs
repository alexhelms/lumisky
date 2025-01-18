using CommunityToolkit.Mvvm.ComponentModel;

namespace LumiSky.Core.Profile;

public interface IPublishSettings : ISettings
{
    bool EnablePublish { get; set; }
    bool PublishImage { get; set; }
    bool PublishPanorama { get; set; }
    bool PublishNightTimelapse { get; set; }
    bool PublishDayTimelapse { get; set; }
    bool ShowPublishedImage { get; set; }
    bool ShowPublishedPanorama { get; set; }
    bool ShowPublishedNightTimelapse { get; set; }
    bool ShowPublishedDayTimelapse { get; set; }

    string Title { get; set; }

    string CfWorkerApiKey { get; set; }
    string CfWorkerUrl { get; set; }
}

public sealed partial class PublishSettings : Settings, IPublishSettings
{
    protected override void Reset()
    {
        EnablePublish = false;
        PublishImage = true;
        PublishPanorama = true;
        PublishNightTimelapse = true;
        PublishDayTimelapse = false;
        ShowPublishedImage = true;
        ShowPublishedPanorama = true;
        ShowPublishedNightTimelapse= true;
        ShowPublishedDayTimelapse = false;
        Title = "LumiSky All Sky";
        CfWorkerApiKey = string.Empty;
        CfWorkerUrl = string.Empty;
    }

    [ObservableProperty]
    public partial bool EnablePublish { get; set; }

    [ObservableProperty]
    public partial bool PublishImage { get; set; }

    [ObservableProperty]
    public partial bool PublishPanorama { get; set; }

    [ObservableProperty]
    public partial bool PublishNightTimelapse { get; set; }

    [ObservableProperty]
    public partial bool PublishDayTimelapse { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CfWorkerApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CfWorkerUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowPublishedImage { get; set; }

    [ObservableProperty]
    public partial bool ShowPublishedPanorama { get; set; }

    [ObservableProperty]
    public partial bool ShowPublishedNightTimelapse { get; set; }

    [ObservableProperty]
    public partial bool ShowPublishedDayTimelapse { get; set; }
}
