using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defSwitch")]
public record DefSwitch : IIndiCommand
{
    [XmlAttribute("device")]
    public required string Device { get; init; }

    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlAttribute("label")]
    public string? Label { get; init; }

    [XmlText]
    public required string Value
    {
        get => field;
        init => field = value.Trim();
    }
}
