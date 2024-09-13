using OdinEye.Core.Profile;
using OdinEye.Core.Services;

namespace OdinEye.Tests;

public class SunServiceTests
{
    private IProfileProvider Profile
    {
        get
        {
            var profile = Mock.Of<IProfileProvider>();
            Mock.Get(profile).Setup(x => x.Current.Location.Latitude).Returns(31.867);
            Mock.Get(profile).Setup(x => x.Current.Location.Longitude).Returns(-109.516);
            return profile;
        }
    }

    [Fact]
    public void GetSunAltitude()
    {
        var sunService = new SunService(Profile);

        var utcDateTime = new DateTime(2024, 9, 12, 23, 35, 7, DateTimeKind.Utc);
        var altitude = sunService.GetSunAltitude(utcDateTime);
        altitude.Should().BeApproximately(23.052, .001);

        var localDateTime = utcDateTime.ToLocalTime();
        altitude = sunService.GetSunAltitude(utcDateTime);
        altitude.Should().BeApproximately(23.052, .001);
    }

    [Fact]
    public void GetSunTimes()
    {
        var sunService = new SunService(Profile);

        var utcDateTime = new DateTime(2024, 9, 12, 21, 0, 0, DateTimeKind.Utc);
        var times = sunService.GetSunTimes(DateOnly.FromDateTime(utcDateTime));
        times.Dawn.Should().BeCloseTo(new DateTime(2024, 9, 11, 12, 36, 45, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
        times.Dusk.Should().BeCloseTo(new DateTime(2024, 9, 12, 1, 55, 10, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
        times = sunService.GetSunTimes(DateOnly.FromDateTime(utcDateTime.ToLocalTime()));
        times.Dawn.Should().BeCloseTo(new DateTime(2024, 9, 11, 12, 36, 45, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
        times.Dusk.Should().BeCloseTo(new DateTime(2024, 9, 12, 1, 55, 10, DateTimeKind.Utc), TimeSpan.FromSeconds(5));

        utcDateTime = new DateTime(2024, 9, 13, 2, 0, 0, DateTimeKind.Utc);
        times = sunService.GetSunTimes(DateOnly.FromDateTime(utcDateTime));
        times.Dawn.Should().BeCloseTo(new DateTime(2024, 9, 12, 12, 37, 23, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
        times.Dusk.Should().BeCloseTo(new DateTime(2024, 9, 13, 1, 53, 49, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
        times = sunService.GetSunTimes(DateOnly.FromDateTime(utcDateTime.ToLocalTime()));
        times.Dawn.Should().BeCloseTo(new DateTime(2024, 9, 11, 12, 36, 45, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
        times.Dusk.Should().BeCloseTo(new DateTime(2024, 9, 12, 1, 55, 10, DateTimeKind.Utc), TimeSpan.FromSeconds(5));
    }
}
