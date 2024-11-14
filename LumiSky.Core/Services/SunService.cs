using LumiSky.Core.Mathematics;
using LumiSky.Core.Profile;

namespace LumiSky.Core.Services;

public record SunTime(DateTime Rise, DateTime Set);

public class SunService
{
    private readonly IProfileProvider _profile;

    public SunService(IProfileProvider profile)
    {
        _profile = profile;
    }

    public bool IsDaytime
    {
        get
        {
            var (alt, _) = GetPosition(DateTime.UtcNow);
            return alt >= _profile.Current.Location.TransitionSunAltitude;
        }
    }

    public bool IsNighttime => !IsDaytime;

    public (double Altitude, double Azimuth) GetPosition(DateTime date) =>
        GetPosition(date, _profile.Current.Location.Latitude, _profile.Current.Location.Longitude);

    public (double Altitude, double Azimuth) GetPosition(DateTime date, double latitude, double longitude)
    {
        latitude = Math.Clamp(latitude, -90, 90);
        longitude = Math.Clamp(longitude, -180, 180);

        var lonWest = -1 * longitude * LumiSkyMath.Deg2Rad;
        var lat = latitude * LumiSkyMath.Deg2Rad;
        var d = Calendar.DaysSinceJ2000(date);

        var (ra, dec) = Sun.Coordinates(d);
        var hourAngle = Position.SiderealTime(d, lonWest) - ra;
        var az = Position.Azimuth(hourAngle, lat, dec) * LumiSkyMath.Rad2Deg;
        var alt = Position.Altitude(hourAngle, lat, dec) * LumiSkyMath.Rad2Deg;

        return (alt, az);
    }

    public SunTime? GetRiseSetTime(DateTime date) =>
        GetRiseSetTime(
            date,
            _profile.Current.Location.TransitionSunAltitude,
            _profile.Current.Location.Latitude,
            _profile.Current.Location.Longitude);

    public SunTime? GetRiseSetTime(DateTime date, double sunAngle) =>
        GetRiseSetTime(
            date,
            sunAngle,
            _profile.Current.Location.Latitude,
            _profile.Current.Location.Longitude);

    public SunTime? GetRiseSetTime(DateTime date, double sunAngle, double latitude, double longitude)
    {
        sunAngle = Math.Clamp(sunAngle, -90, 90);
        latitude = Math.Clamp(latitude, -90, 90);
        longitude = Math.Clamp(longitude, -180, 180);

        var lonWest = -1 * longitude * LumiSkyMath.Deg2Rad;
        var lat = latitude * LumiSkyMath.Deg2Rad;
        var d = Calendar.DaysSinceJ2000(date);
        var n = Sun.JulianCycle(d, lonWest);
        var ds = Sun.ApproxTransit(0, lonWest, n);

        var meanAnomaly = Sun.MeanAnomaly(ds);
        var eclipticLon = Sun.EclipticLongitude(meanAnomaly);
        var solarDec = Position.Declination(eclipticLon, 0);

        var jdNoon = Sun.SolarTransitJD(ds, meanAnomaly, eclipticLon);

        var sunAngleToHorizon = sunAngle * LumiSkyMath.Deg2Rad;
        var jdSet = Sun.GetSetJD(sunAngleToHorizon, lonWest, lat, solarDec, n, meanAnomaly, eclipticLon);
        if (double.IsNaN(jdSet))
            return null;

        var jdRise = jdNoon - (jdSet - jdNoon);

        return new SunTime(
            Rise: Calendar.FromJulian(jdRise),
            Set: Calendar.FromJulian(jdSet));
    }

    private static class Calendar
    {
        public const double DayMilliseconds = 86400000;
        public const double J1970 = 2440588;
        public const double J2000 = 2451545;

        public static double ToJulian(DateTime date) =>
            new DateTimeOffset(date).ToUniversalTime().ToUnixTimeMilliseconds() / DayMilliseconds - 0.5 + J1970;

        public static DateTime FromJulian(double jd) =>
            DateTimeOffset.FromUnixTimeMilliseconds((long)((jd + 0.5 - J1970) * DayMilliseconds)).UtcDateTime;

        public static double DaysSinceJ2000(DateTime date) =>
            ToJulian(date) - J2000;
    }

    private static class Position
    {
        private const double EarthObliquity = 23.4393 * LumiSkyMath.Deg2Rad;

        public static double RightAscension(double eclipticalLon, double eclipticalLat) =>
            Math.Atan2(Math.Sin(eclipticalLon) * Math.Cos(EarthObliquity) - Math.Tan(eclipticalLat) * Math.Sin(EarthObliquity), Math.Cos(eclipticalLon));

        public static double Declination(double eclipticalLon, double eclipticalLat) =>
            Math.Asin(Math.Sin(eclipticalLat) * Math.Cos(EarthObliquity) + Math.Cos(eclipticalLat) * Math.Sin(EarthObliquity) * Math.Sin(eclipticalLon));

        public static double Azimuth(double hourAngle, double geoLat, double solarDec) =>
            Math.Atan2(Math.Sin(hourAngle), Math.Cos(hourAngle) * Math.Sin(geoLat) - Math.Tan(solarDec) * Math.Cos(geoLat));

        public static double Altitude(double hourAngle, double geoLat, double solardec) =>
            Math.Asin(Math.Sin(geoLat) * Math.Sin(solardec) + Math.Cos(geoLat) * Math.Cos(solardec) * Math.Cos(hourAngle));

        public static double SiderealTime(double daysSinceJ2000, double lonWest) =>
            LumiSkyMath.Deg2Rad * (280.1470 + 360.9856235 * daysSinceJ2000) - lonWest;
    }

    private static class Sun
    {
        // Reference: http://aa.quae.nl/en/reken/zonpositie.html

        public const double EarthEclipticLongitude = 102.9373;

        // Constants for Equation of Center for the Earth
        public const double C1 = 1.9148;
        public const double C2 = 0.0200;
        public const double C3 = 0.0003;

        // Correction coefficients for transit time
        public const double J0 = 0.0009;
        public const double J1 = 0.0053;
        public const double J2 = -0.0068;

        public static double MeanAnomaly(double daysSinceJ2000) =>
            (357.5291 + 0.98560028 * daysSinceJ2000) * LumiSkyMath.Deg2Rad;

        public static double EclipticLongitude(double meanAnomaly)
        {
            var equationOfCenter = (C1 * Math.Sin(meanAnomaly) + C2 * Math.Sin(2*meanAnomaly) + C3 * Math.Sin(3*meanAnomaly)) * LumiSkyMath.Deg2Rad;
            var earthPerihelion = EarthEclipticLongitude * LumiSkyMath.Deg2Rad;
            return meanAnomaly + equationOfCenter + earthPerihelion + Math.PI;
        }

        public static (double RightAscension, double Declination) Coordinates(double daysSinceJ2000)
        {
            var meanAnomaly = MeanAnomaly(daysSinceJ2000);
            var eclipticLon = EclipticLongitude(meanAnomaly);
            var declination = Position.Declination(eclipticLon, 0);
            var rightAscension = Position.RightAscension(eclipticLon, 0);
            return (rightAscension, declination);
        }

        public static double SolarTransitJD(double daysSinceJ2000, double meanAnomaly, double eclipticLon) =>
            Calendar.J2000 + daysSinceJ2000 + (J1 * Math.Sin(meanAnomaly)) + (J2 * Math.Sin(2 * eclipticLon));

        public static double HourAngle(double sunAngleToHorizon, double geoLat, double solarDec) =>
            Math.Acos((Math.Sin(sunAngleToHorizon) - Math.Sin(geoLat) * Math.Sin(solarDec)) / (Math.Cos(geoLat) * Math.Cos(solarDec)));

        public static double JulianCycle(double daysSinceJ2000, double lonWest) =>
            Math.Round(daysSinceJ2000 - J0 - (lonWest / (2 * Math.PI)));

        public static double ApproxTransit(double hourAngle, double lonWest, double n) =>
            J0 + (hourAngle + lonWest) / (2 * Math.PI) + n;

        public static double GetSetJD(double sunAngleToHorizon, double lonWest, double geoLat, double solarDec, double n, double meanAnomaly, double meanLongitude)
        {
            var w = HourAngle(sunAngleToHorizon, geoLat, solarDec);
            var a = ApproxTransit(w, lonWest, n);
            return SolarTransitJD(a, meanAnomaly, meanLongitude);
        }
    }
}
