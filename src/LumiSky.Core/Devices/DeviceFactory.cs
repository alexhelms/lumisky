using LumiSky.Core.Profile;

namespace LumiSky.Core.Devices;

public class DeviceFactory
{
    private readonly IProfileProvider _profile;

    private IndiCamera? _camera;

    public DeviceFactory(IProfileProvider profile)
    {
        _profile = profile;
    }

    public async Task<IndiCamera> GetOrCreateConnectedCamera(CancellationToken token)
    {
        _camera ??= new IndiCamera(_profile);

        if (!_camera.IsConnected)
        {
            await _camera.ConnectAsync(token);
        }

        return _camera;
    }

    public void DestroyCamera()
    {
        _camera?.Dispose();
        _camera = null;
    }
}
