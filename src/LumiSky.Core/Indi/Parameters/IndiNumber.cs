using LumiSky.Core.Indi.Serialization;
using System.Text;

namespace LumiSky.Core.Indi.Parameters;

public class IndiNumberVector : IndiVector
{
    public IndiNumberVector(DefNumberVector def)
        : base(def)
    {
        Items = def.Items
            .Select(x => new IndiNumber
            {
                Name = x.Name,
                Label = x.Label,
                Format = x.Format,
                Min = ParseRawNumber(x.Min, x.Format),
                Max = ParseRawNumber(x.Max, x.Format),
                Step = ParseRawNumber(x.Step, x.Format),
                Value = ParseRawNumber(x.Value, x.Format),
            })
            .ToDictionary(x => x.Name);
    }

    public Dictionary<string, IndiNumber> Items { get; private set; } = [];

    public void Update(SetNumberVector command)
    {
        Timeout = command.Timeout;
        Timestamp = ParseRawTimestamp(command.Timestamp);
        State = command.State;

        foreach (var newItem in command.Items)
        {
            if (Items.TryGetValue(newItem.Name, out var item))
            {
                item.Value = ParseRawNumber(newItem.Value, item.Format);
            }
        }
    }

    public override IIndiCommand ToCommand(IEnumerable<(string, object)> values)
    {
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss");
        var newVector = new NewNumberVector
        {
            Device = Device,
            Name = Name,
            Timestamp = timestamp,
            Items = values
                .Select(x => new OneNumber
                {
                    Name = x.Item1,
                    Value = Convert.ToDouble(x.Item2).ToString(),
                })
                .ToList(),
        };
        return newVector;
    }
}

public class IndiNumber : IIndiValue, IEquatable<IndiNumber>
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public required string Format { get; init; }
    public required double Min { get; init; }
    public required double Max { get; init; }
    public required double Step { get; init; }
    public required double Value { get; set; }

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
        return Equals(obj as IndiNumber);
    }

    public bool Equals(IndiNumber? other)
    {
        return other is not null &&
               Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public static bool operator ==(IndiNumber? left, IndiNumber? right)
    {
        return EqualityComparer<IndiNumber>.Default.Equals(left, right);
    }

    public static bool operator !=(IndiNumber? left, IndiNumber? right)
    {
        return !(left == right);
    }

    #endregion
}