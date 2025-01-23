using LumiSky.Core.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace LumiSky.Core.Devices;

public class DeviceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProfileProvider _profile;

    public DeviceFactory(
        IServiceProvider serviceProvider,
        IProfileProvider profile)
    {
        _serviceProvider = serviceProvider;
        _profile = profile;
    }

    public async Task<ICamera> GetCamera(CancellationToken token)
    {
        var cameraType = _profile.Current.Camera.CameraType;
        ICamera camera = cameraType switch
        {
            DeviceTypes.INDI => _serviceProvider.GetRequiredService<IndiCamera>(),
            DeviceTypes.RaspiWeb => _serviceProvider.GetRequiredService<RaspiWebCamera>(),
            DeviceTypes.RaspiNative => _serviceProvider.GetRequiredService<RaspiNativeCamera>(),
            _ => throw new InvalidOperationException($"Unsupported camera type: {cameraType}"),
        };

        await camera.ConnectAsync(token);

        return camera;
    }
}
