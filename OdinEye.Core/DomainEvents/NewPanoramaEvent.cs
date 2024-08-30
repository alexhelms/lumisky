using OdinEye.Core.Primitives;

namespace OdinEye.Core.DomainEvents;

public record NewPanoramaEvent
{
    public required string Filename { get; init; }
    public required Size Size { get; init; }
}
