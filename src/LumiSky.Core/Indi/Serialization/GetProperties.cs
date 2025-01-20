using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("getProperties")]
public record GetProperties : IIndiCommand
{
    [XmlAttribute("version")]
    public string Version { get; init; } = "1.7";

    [XmlAttribute("device")]
    public string? Device { get; init; }

    [XmlAttribute("name")]
    public string? Name { get; init; }
}
