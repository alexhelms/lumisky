using LumiSky.Core.Imaging;

namespace LumiSky.Core.Devices;

public interface ICamera : IDisposable
{
    Task<bool> ConnectAsync(CancellationToken token = default);
    void Disconnect();

    Task<AllSkyImage?> TakeImageAsync(ExposureParameters parameters, CancellationToken token = default);
    Task AbortImageAsync();

    public string DeviceType { get; }
    public string Name { get; }
    public bool IsConnected { get; }
}
