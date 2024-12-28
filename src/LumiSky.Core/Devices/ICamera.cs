using LumiSky.Core.Imaging;

namespace LumiSky.Core.Devices;

public interface ICamera
{
    Task<bool> ConnectAsync(CancellationToken token = default);
    Task DisconnectAsync();

    Task<AllSkyImage?> TakeImageAsync(ExposureParameters parameters, CancellationToken token = default);
    Task AbortImageAsync();

    public string Name { get; }
    public bool IsConnected { get; }
    public int Gain { get; }
    public int GainMin { get; }
    public int GainMax { get; }
    public int Offset { get; }
    public int OffsetMin { get; }
    public int OffsetMax { get; }
    public double PixelSize { get; }
    public BayerPattern BayerPattern { get; }
}
