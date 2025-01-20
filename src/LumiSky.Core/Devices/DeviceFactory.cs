using LumiSky.Core.Profile;

namespace LumiSky.Core.Devices;

public class DeviceFactory
{
    private readonly IProfileProvider _profile;

    public DeviceFactory(IProfileProvider profile)
    {
        _profile = profile;
    }

    public async Task<IndiCamera> GetCamera(CancellationToken token)
    {
        var camera = new IndiCamera(_profile);
        await camera.ConnectAsync(token);
        return camera;
    }
}
