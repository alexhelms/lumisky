using OdinEye.Core.Primitives;

namespace OdinEye.Core.DomainEvents;

public record NewImageEvent
{
    public required string Filename { get; init; }
    public required Size Size { get; init; }
}
