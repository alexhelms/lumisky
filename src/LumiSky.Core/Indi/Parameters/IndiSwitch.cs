using LumiSky.Core.Indi.Serialization;
using System.Text;

namespace LumiSky.Core.Indi.Parameters;

public class IndiSwitchVector : IndiVector
{
    public IndiSwitchVector(DefSwitchVector def)
        : base(def)
    {
        Rule = def.Rule;
        Items = def.Items
            .Select(x => new IndiSwitch
            {
                Name = x.Name,
                Label = x.Label,
                Value = ParseRawSwitchState(x.Value) == SwitchState.On,
            })
            .ToDictionary(x => x.Name);
    }

    public SwitchRule Rule { get; private set; }
    public Dictionary<string, IndiSwitch> Items { get; private set; } = [];

    public void Update(SetSwitchVector command)
    {
        Timeout = command.Timeout;
        Timestamp = ParseRawTimestamp(command.Timestamp);
        State = command.State;
        
        foreach (var newItem in command.Items)
        {
            if (Items.TryGetValue(newItem.Name, out var item))
            {
                item.Value = ParseRawSwitchState(newItem.Value) == SwitchState.On;
            }
        }
    }

    public override IIndiCommand ToCommand(IEnumerable<(string, object)> values)
    {
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("yyyy-MM-dd") + "T" + now.ToString("HH:mm:ss");
        var newVector = new NewSwitchVector
        {
            Device = Device,
            Name = Name,
            Timestamp = timestamp,
            Items = values
                .Select(x => new OneSwitch
                {
                    Name = x.Item1,
                    Value = GetValue(x.Item2).ToString(),
                })
                .ToList(),
        };
        return newVector;

        static SwitchState GetValue(object obj) 
            => obj switch
            {
                SwitchState state => state,
                bool b => b ? SwitchState.On : SwitchState.Off,
                int i => i == 0 ? SwitchState.Off : SwitchState.On,
                _ => throw new InvalidCastException($"Cannot infer switch state: {obj}"),
            };
    }
}

public class IndiSwitch : IIndiValue, IEquatable<IndiSwitch>
{
    public required string Name { get; init; }
    public string? Label { get; init; }
    public bool Value { get; set; }

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
        return Equals(obj as IndiSwitch);
    }

    public bool Equals(IndiSwitch? other)
    {
        return other is not null &&
               Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public static bool operator ==(IndiSwitch? left, IndiSwitch? right)
    {
        return EqualityComparer<IndiSwitch>.Default.Equals(left, right);
    }

    public static bool operator !=(IndiSwitch? left, IndiSwitch? right)
    {
        return !(left == right);
    }

    #endregion
}
