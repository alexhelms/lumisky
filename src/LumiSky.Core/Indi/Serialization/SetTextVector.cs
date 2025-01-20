using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("setTextVector")]
public record SetTextVector : SetVector
{
    [XmlElement("oneText")]
    public List<OneText> Items { get; init; } = [];

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
