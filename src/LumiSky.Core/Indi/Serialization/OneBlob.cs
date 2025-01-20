using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("oneBLOB")]
public record OneBlob : IXmlSerializable
{
    public string Name { get; private set; } = string.Empty;

    public int Size { get; private set; }

    public string Format { get; private set; } = string.Empty;

    public Memory<byte> Value { get; private set; } = Array.Empty<byte>();

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer) { }

    public void ReadXml(XmlReader reader)
    {
        reader.MoveToContent();

        // "format" varies, can be .bin, .z, .fits.z ...
        // TODO: do something with the format

        Name = reader.GetAttribute("name") ?? string.Empty;
        Size = int.Parse(reader.GetAttribute("size") ?? "0");
        Format = reader.GetAttribute("format") ?? string.Empty;
        Value = ParseBlob(reader, Format, Size);

        // Get to the end of the element.
        reader.Read();
    }

    private static Memory<byte> ParseBlob(XmlReader reader, string format, int size)
    {
        byte[] base64Buffer = new byte[65536];
        byte[] blobBuffer = new byte[size];  // TODO: more sophisticated memory management for large blobs

        // Move to the element contents
        reader.Read();

        int readBytes = 0, bytesOffset = 0;
        while ((readBytes = reader.ReadContentAsBase64(base64Buffer, 0, base64Buffer.Length)) > 0)
        {
            var srcSpan = base64Buffer.AsSpan()[..readBytes];
            var dstSpan = blobBuffer.AsSpan()[bytesOffset..];
            srcSpan.CopyTo(dstSpan);
            bytesOffset += readBytes;
        }

        return blobBuffer;
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.AppendFormat("{0} = {1}, ", nameof(Name), Name);
        builder.AppendFormat("{0} = {1}, ", nameof(Size), Size);
        builder.AppendFormat("{0} = {1}", nameof(Format), Format);
        return true;
    }
}
