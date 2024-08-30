namespace OdinEye.Core.Devices;

public record ExposureParameters
{
    public TimeSpan Duration { get; init; }
    public int Gain { get; init; }
    public int Offset { get; init; }
}
