using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("enableBLOB")]
public record EnableBlob : IIndiCommand
{
    [XmlAttribute("device")]
    public string? Device { get; init; }

    [XmlAttribute("name")]
    public string? Name { get; init; }

    [XmlText]
    public BlobEnable State { get; init; }
}
