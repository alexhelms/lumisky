using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defNumberVector")]
public record DefNumberVector : DefVector
{
    [XmlElement("defNumber")]
    public List<DefNumber> Items { get; init; } = [];

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", Items = [");
            builder.AppendJoin(", ", Items.Select(x => $"{x.Name} = {x.Value}"));
            builder.Append(']');
        }

        return true;
    }
}
