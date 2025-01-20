using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("oneNumber")]
public record OneNumber
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlText]
    public required string Value
    {
        get => field;
        init => field = value.Trim();
    }
}
