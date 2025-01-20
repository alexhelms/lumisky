using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("newBLOBVector")]
public record NewBlobVector : NewVector
{
    [XmlElement("oneBlob")]
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
