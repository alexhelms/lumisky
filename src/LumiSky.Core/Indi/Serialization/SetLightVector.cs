using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("setLightVector")]
public record SetLightVector : SetVector
{
    [XmlElement("oneLight")]
    public List<OneLight> Items { get; init; } = [];

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
