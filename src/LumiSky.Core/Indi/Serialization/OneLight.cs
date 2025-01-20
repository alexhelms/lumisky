using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("oneLight")]
public record OneLight
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlText]
    public required PropertyState Value { get; init; }
}
