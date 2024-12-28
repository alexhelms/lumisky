using LumiSky.Core.Primitives;

namespace LumiSky.Core.DomainEvents;

public record NewPanoramaEvent
{
    public required string Filename { get; init; }
    public required Size Size { get; init; }
}
