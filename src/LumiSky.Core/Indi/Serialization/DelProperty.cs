using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("delProperty")]
public record DelProperty : IIndiCommand, IHasDeviceName
{
    [XmlAttribute("device")]
    public required string Device { get; init; }

    [XmlAttribute("name")]
    public string? Name { get; init; }

    [XmlAttribute("timestamp")]
    public string? Timestamp { get; init; }

    [XmlAttribute("message")]
    public string? Content { get; init; }
}
