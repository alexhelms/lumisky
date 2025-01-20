using LumiSky.Core.Indi.Serialization;
using System.Text;

namespace LumiSky.Core.Indi.Parameters;

public class IndiLightVector : IndiVector
{
    public IndiLightVector(DefLightVector def)
        : base(def)
    {
        Items = def.Items
            .Select(x => new IndiLight
            {
                Name = x.Name,
                Label = x.Label,
            })
            .ToDictionary(x => x.Name);
    }

    public Dictionary<string, IndiLight> Items { get; private set; } = [];

    public void Update(SetLightVector command)
    {
        Timeout = command.Timeout;
        Timestamp = ParseRawTimestamp(command.Timestamp);
        State = command.State;

        foreach (var newItem in command.Items)
        {
            if (Items.TryGetValue(newItem.Name, out var item))
            {
                item.Value = newItem.Value;
            }
        }
    }

    public override IIndiCommand ToCommand(IEnumerable<(string, object)> values)
    {
        throw new NotSupportedException();
    }
}

public class IndiLight : IIndiValue, IEquatable<IndiLight>
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public PropertyState Value { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder(128);
        builder.Append(GetType().Name);
        builder.Append(" { ");
        builder.AppendFormat("{0} = {1}", Name, Value);
        builder.Append(" }");
        return builder.ToString();
    }

    #region Equality

    public override bool Equals(object? obj)
    {
        return Equals(obj as IndiLight);
    }

    public bool Equals(IndiLight? other)
    {
        return other is not null &&
               Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public static bool operator ==(IndiLight? left, IndiLight? right)
    {
        return EqualityComparer<IndiLight>.Default.Equals(left, right);
    }

    public static bool operator !=(IndiLight? left, IndiLight? right)
    {
        return !(left == right);
    }

    #endregion
}
