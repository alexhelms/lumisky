using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defBLOB")]
public record DefBlob : IIndiCommand
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlAttribute("label")]
    public string? Label { get; init; }
}
