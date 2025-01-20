using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defNumber")]
public record DefNumber : IIndiCommand
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlAttribute("label")]
    public string? Label { get; init; }

    [XmlAttribute("format")]
    public required string Format { get; init; }

    [XmlAttribute("min")]
    public required string Min { get; init; }

    [XmlAttribute("max")]
    public required string Max { get; init; }

    [XmlAttribute("step")]
    public required string Step { get; init; }

    [XmlText]
    public required string Value
    {
        get => field;
        init => field = value.Trim();
    }
}
