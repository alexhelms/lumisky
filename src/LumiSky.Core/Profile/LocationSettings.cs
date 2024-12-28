using CommunityToolkit.Mvvm.ComponentModel;

namespace LumiSky.Core.Profile;

public interface ILocationSettings : ISettings
{
    string Location { get; set; }
    double Latitude { get; set; }
    double Longitude { get; set; }
    double Elevation { get; set; }
    double TransitionSunAltitude { get; set; }
}

public sealed partial class LocationSettings : Settings, ILocationSettings
{
    protected override void Reset()
    {
        Location = string.Empty;
        Latitude = 0;
        Longitude = 0;
        Elevation = 0;
        TransitionSunAltitude = -6;
    }

    [ObservableProperty]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Latitude { get; set; }

    [ObservableProperty]
    public partial double Longitude { get; set; }

    [ObservableProperty]
    public partial double Elevation { get; set; }

    [ObservableProperty]
    public partial double TransitionSunAltitude { get; set; }
}
