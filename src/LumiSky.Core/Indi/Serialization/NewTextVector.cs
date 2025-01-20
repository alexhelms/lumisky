using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("newTextVector")]
public record NewTextVector : NewVector
{
    [XmlElement("oneText")]
    public List<OneText> Items { get; init; } = [];

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", Items = [");
            builder.AppendJoin(", ", Items.Select(x => x.Name));
            builder.Append(']');
        }

        return true;
    }
}
