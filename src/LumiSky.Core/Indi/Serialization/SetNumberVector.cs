using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("setNumberVector")]
public record SetNumberVector : SetVector
{
    [XmlElement("oneNumber")]
    public List<OneNumber> Items { get; init; } = [];

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
