using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

[XmlRoot("defSwitchVector")]
public record DefSwitchVector : DefVector
{
    [XmlAttribute("rule")]
    public SwitchRule Rule { get; init; }

    [XmlElement("defSwitch")]
    public List<DefSwitch> Items { get; init; } = [];

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.AppendFormat("{0} = {1}, ", nameof(Rule), Rule);
            builder.Append(", Items = [");
            builder.AppendJoin(", ", Items.Select(x => $"{x.Name} = {x.Value}"));
            builder.Append(']');
        }

        return true;
    }
}
