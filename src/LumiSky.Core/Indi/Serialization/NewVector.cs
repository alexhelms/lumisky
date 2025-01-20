using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

public abstract record NewVector : IIndiCommand, IHasDeviceName
{
    [XmlAttribute("device")]
    public required string Device { get; init; }

    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlAttribute("timestamp")]
    public string? Timestamp { get; set; }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.AppendFormat("{0} = {1}, ", nameof(Device), Device);
        builder.AppendFormat("{0} = {1}, ", nameof(Name), Name);
        builder.AppendFormat("{0} = {1} ", nameof(Timestamp), Timestamp);
        return true;
    }
}
