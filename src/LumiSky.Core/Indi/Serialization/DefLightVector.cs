using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defLightVector")]
public record DefLightVector : DefVector
{
    [XmlElement("defLight")]
    public List<DefLight> Items { get; init; } = [];

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
