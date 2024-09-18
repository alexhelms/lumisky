using CommunityToolkit.Mvvm.ComponentModel;

namespace OdinEye.Core.Profile;

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

    [ObservableProperty] string _location = string.Empty;
    [ObservableProperty] double _latitude;
    [ObservableProperty] double _longitude;
    [ObservableProperty] double _elevation;
    [ObservableProperty] double _transitionSunAltitude;
}
