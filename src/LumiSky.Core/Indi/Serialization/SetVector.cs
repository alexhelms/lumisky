using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi.Serialization;

public abstract record SetVector : IIndiCommand, IHasDeviceName
{
    [XmlAttribute("device")]
    public required string Device { get; init; }

    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlAttribute("state")]
    public PropertyState State { get; init; }

    [XmlAttribute("timeout")]
    public int Timeout { get; init; }

    [XmlAttribute("timestamp")]
    public string? Timestamp { get; init; }

    [XmlAttribute("message")]
    public string? Message { get; init; }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.AppendFormat("{0} = {1}, ", nameof(Device), Device);
        builder.AppendFormat("{0} = {1}, ", nameof(Name), Name);
        builder.AppendFormat("{0} = {1}, ", nameof(State), State);
        builder.AppendFormat("{0} = {1}, ", nameof(Timeout), Timeout);
        builder.AppendFormat("{0} = {1}, ", nameof(Timestamp), Timestamp);
        builder.AppendFormat("{0} = {1}", nameof(Message), Message);
        return true;
    }
}
