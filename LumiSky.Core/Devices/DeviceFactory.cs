using LumiSky.Core.Profile;

namespace LumiSky.Core.Devices;

public class DeviceFactory
{
    private readonly IProfileProvider _profile;

    public DeviceFactory(IProfileProvider profile)
    {
        _profile = profile;
    }

    public IndiCamera CreateCamera()
    {
        // This is pretty trivial now but LumiSky will eventually support
        // other camera kinds and will need another abstraction over all
        // cameras, aka. an LumiSkyCamera that takes a ICamera so all
        // downstream callers have the same interface.

        var camera = new IndiCamera(_profile);
        return camera;
    }
}
