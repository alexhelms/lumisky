using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("message")]
public record Message : IXmlSerializable, IIndiCommand
{
    public string? Device { get; private set; }

    public DateTime Timestamp { get; private set; }

    public string? Content { get; private set; }

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer) { }

    public void ReadXml(XmlReader reader)
    {
        // <message /> is a an empty element, no need to advance the reader.

        // I am manually deserializing <message /> because using the attributes
        // results in the next element sometimes being missed. I'm probably
        // misconfiguring something but I couldn't figure it out and this
        // is an easy enough work around.

        var xmlTimestamp = reader.GetAttribute("timestamp");
        DateTime.TryParse(xmlTimestamp, null, DateTimeStyles.RoundtripKind, out var timestamp);
        Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        Device = reader.GetAttribute("device");
        Content = reader.GetAttribute("message");
    }
}
