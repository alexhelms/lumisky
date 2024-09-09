using OdinEye.Core.Profile;
using SunCalcSharp;

namespace OdinEye.Core.Services;

public class SunService
{
    private readonly IProfileProvider _profile;

    public SunService(IProfileProvider profile)
    {
        _profile = profile;
    }

    public bool IsDaytime => GetSunAltitude() >= DayNightTransitionAltitude;

    public bool IsNighttime => GetSunAltitude() <= DayNightTransitionAltitude;

    public double DayNightTransitionAltitude => -6; // Dusk/Dawn

    public double GetSunAltitude()
        => GetSunAltitude(DateTime.Now);

    public double GetSunAltitude(DateTime date)
        => GetSunAltitude(date, _profile.Current.Location.Latitude, _profile.Current.Location.Longitude);

    /// <summary>
    /// Get the sun altitude in degrees.
    /// </summary>
    /// <param name="date">Date and time.</param>
    /// <param name="latitude">Latitude, degrees, +/- 90, north positive.</param>
    /// <param name="longitude">Longitude, degrees, +/- 180, east positive.</param>
    /// <returns>The sun altitude in degrees.</returns>
    public double GetSunAltitude(DateTime date, double latitude, double longitude)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);

        var position = SunCalc.GetPosition(date, latitude, longitude);
        return position.Altitude * 180.0 / Math.PI;
    }

    public SunTimes GetSunTimes(DateOnly date, double latitude, double longitude) =>
        SunCalc.GetTimes(new DateTime(date, TimeOnly.MinValue), latitude, longitude);

    public SunTimes GetSunTimes(DateOnly date) => GetSunTimes(date, _profile.Current.Location.Latitude, _profile.Current.Location.Longitude);

}
