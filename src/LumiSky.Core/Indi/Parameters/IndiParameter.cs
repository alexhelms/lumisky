using LumiSky.Core.Indi.Serialization;
using System.Text;

namespace LumiSky.Core.Indi.Parameters;

public class IndiParameter
{
    private readonly IndiVector _vector;

    public event EventHandler? Updated;

    public IndiParameter(DefVector defVector)
    {
        IndiVector vector = defVector switch
        {
            DefBlobVector defBlobVector => new IndiBlobVector(defBlobVector),
            DefTextVector defTextVector => new IndiTextVector(defTextVector),
            DefNumberVector defNumberVector => new IndiNumberVector(defNumberVector),
            DefSwitchVector defSwitchVector => new IndiSwitchVector(defSwitchVector),
            DefLightVector defLightVector => new IndiLightVector(defLightVector),
            _ => throw new NotImplementedException(),
        };

        _vector = vector;
    }

    public void SetBusy()
    {
        _vector.State = PropertyState.Busy;
    }

    public void Update(SetVector command)
    {
        Action action = (_vector, command) switch
        {
            (IndiBlobVector vector, SetBlobVector set) => () => vector.Update(set),
            (IndiTextVector vector, SetTextVector set) => () => vector.Update(set),
            (IndiNumberVector vector, SetNumberVector set) => () => vector.Update(set),
            (IndiSwitchVector vector, SetSwitchVector set) => () => vector.Update(set),
            (IndiLightVector vector, SetLightVector set) => () => vector.Update(set),
            _ => static () => { },
        };

        action();

        Updated?.Invoke(this, EventArgs.Empty);
    }

    public IIndiCommand ToCommand(IEnumerable<(string, object)> values)
    {
        return  _vector switch
        {
            IndiBlobVector vector => vector.ToCommand(values),
            IndiTextVector vector => vector.ToCommand(values),
            IndiNumberVector vector => vector.ToCommand(values),
            IndiSwitchVector vector => vector.ToCommand(values),
            IndiLightVector vector => vector.ToCommand(values),
            _ => throw new NotImplementedException(),
        };
    }

    public Dictionary<string, TValue> GetItems<TValue>()
        where TValue : IIndiValue
    {
        object items = _vector switch
        {
            IndiBlobVector vector => vector.Items,
            IndiTextVector vector => vector.Items,
            IndiNumberVector vector => vector.Items,
            IndiSwitchVector vector => vector.Items,
            IndiLightVector vector => vector.Items,
            _ => throw new NotImplementedException(),
        };

        return (Dictionary<string, TValue>)items;
    }

    public bool AreItemsEquivalent(IEnumerable<(string, object)> values)
    {
        var names = values.Select(x => x.Item1).ToHashSet();

        if (_vector is IndiNumberVector numberVector)
        {
            const double Epsilon = 1e-8;

            List<double> currentValues = numberVector.Items
                .Where(x => names.Contains(x.Key))
                .Select(x => x.Value)
                .Select(x => x.Value)
                .ToList();

            List<double> newValues = values
                .Select(x => Convert.ToDouble(x.Item2))
                .ToList();

            return currentValues
                .Zip(newValues)
                .Select(pair => Math.Abs(pair.First - pair.Second))
                .All(remainder => remainder < Epsilon);
        }
        else if (_vector is IndiSwitchVector switchVector)
        {
            List<(string, bool)> currentValues = switchVector.Items
                .Where(x => names.Contains(x.Key))
                .Select(x => x.Value)
                .OrderBy(x => x.Name)
                .Select(x => (x.Name.ToLowerInvariant(), x.Value))
                .ToList();

            List<(string, bool)> newValues = values
                .OrderBy(x => x.Item1)
                .Select(x => (x.Item1.ToLowerInvariant(), Convert.ToBoolean(x.Item2)))
                .ToList();

            return Enumerable.SequenceEqual(currentValues, newValues);
        }
        else if (_vector is IndiTextVector textVector)
        {
            List<string> currentValues = textVector.Items
                .Where(x => names.Contains(x.Key))
                .Select(x => x.Value)
                .Select(x => x.Value.ToLowerInvariant())
                .ToList();

            List<string> newValues = values
                .Select(x => x.Item2.ToString()?.ToLowerInvariant() ?? string.Empty)
                .ToList();

            return Enumerable.SequenceEqual(currentValues, newValues);
        }

        return false;
    }

    public override string ToString()
    {
        var builder = new StringBuilder(1024);
        builder.Append(GetType().Name);
        builder.Append(" { ");
        builder.AppendFormat("{0} = {1}, ", nameof(Name), Name);
        builder.AppendFormat("{0} = {1}, ", nameof(Group), Group);
        builder.AppendFormat("{0} = {1}, ", nameof(State), State);
        builder.AppendFormat("{0} = {1}, ", nameof(Permission), Permission);
        builder.AppendFormat("{0} = {1}, ", nameof(Timeout), Timeout);
        builder.AppendFormat("{0} = {1}, ", nameof(Timestamp), Timestamp);
        builder.Append("Items { ");

        Action formatAction = _vector switch
        {
            IndiBlobVector vector => () => FormatItems(builder, vector.Items.Values),
            IndiTextVector vector => () => FormatItems(builder, vector.Items.Values),
            IndiNumberVector vector => () => FormatItems(builder, vector.Items.Values),
            IndiSwitchVector vector => () => FormatItems(builder, vector.Items.Values),
            IndiLightVector vector => () => FormatItems(builder, vector.Items.Values),
            _ => throw new NotImplementedException(),
        };

        formatAction();

        static void FormatItems(StringBuilder builder, IEnumerable<IIndiValue> values)
        {
            builder.AppendJoin(", ", values.Select(x => x.ToString()));
        }

        builder.Append(" }");

        return builder.ToString();
    }

    public string Name => _vector.Name;
    public string? Group => _vector.Group;
    public string? Label => _vector.Label;
    public PropertyState State => _vector.State;
    public PropertyPermission Permission => _vector.Permission;
    public int? Timeout => _vector.Timeout;
    public DateTime? Timestamp => _vector.Timestamp;
}
