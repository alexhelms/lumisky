using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defLight")]
public record DefLight : IIndiCommand
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlAttribute("label")]
    public string? Label { get; init; }
}
