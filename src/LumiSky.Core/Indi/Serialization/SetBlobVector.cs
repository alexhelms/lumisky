using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("setBLOBVector")]
public record SetBlobVector : SetVector
{
    [XmlElement("oneBLOB")]
    public List<OneBlob> Items { get; init; } = [];

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", Items = [");
            builder.AppendJoin(", ", Items.Select(x => x.ToString()));
            builder.Append(']');
        }

        return true;
    }
}
