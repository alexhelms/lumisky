using LumiSky.Core.Indi.Serialization;
using System.Text;

namespace LumiSky.Core.Indi.Parameters;

public class IndiTextVector : IndiVector
{
    public IndiTextVector(DefTextVector def)
        : base(def)
    {
        Items = def.Items
            .Select(x => new IndiText
            {
                Name = x.Name,
                Label = x.Label,
                Value = x.Value,
            })
            .ToDictionary(x => x.Name);
    }

    public Dictionary<string, IndiText> Items { get; private set; } = [];

    public void Update(SetTextVector command)
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
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss");
        var newVector = new NewTextVector
        {
            Device = Device,
            Name = Name,
            Timestamp = timestamp,
            Items = values
                .Select(x => new OneText
                {
                    Name = x.Item1,
                    Value = x.Item2?.ToString() ?? string.Empty,
                })
                .ToList(),
        };
        return newVector;
    }
}

public class IndiText : IIndiValue, IEquatable<IndiText?>
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public string Value { get; set; } = string.Empty;

    public override string ToString()
    {
        var builder = new StringBuilder(128);
        builder.Append(GetType().Name);
        builder.Append(" { ");
        builder.Append(Value);
        builder.Append(" }");
        return builder.ToString();
    }

    #region Equality

    public override bool Equals(object? obj)
    {
        return Equals(obj as IndiText);
    }

    public bool Equals(IndiText? other)
    {
        return other is not null &&
               Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public static bool operator ==(IndiText? left, IndiText? right)
    {
        return EqualityComparer<IndiText>.Default.Equals(left, right);
    }

    public static bool operator !=(IndiText? left, IndiText? right)
    {
        return !(left == right);
    }

    #endregion
}
