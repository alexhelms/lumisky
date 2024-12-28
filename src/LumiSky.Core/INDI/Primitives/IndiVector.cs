using System.Collections;
using System.Xml.Linq;

namespace LumiSky.INDI.Primitives;

public abstract class IndiVector : IndiValue
{
    public string Group { get; set; } = string.Empty;
    public IndiState State { get; set; } = IndiState.Idle;
    public string Permissions { get; set; } = "rw";
    public bool IsReadOnly => Permissions == "ro";
    public bool IsWriteOnly => Permissions == "wo";
    public bool IsReadWrite => Permissions == "rw";
    public bool IsIdle => State == IndiState.Idle;
    public bool IsOk => State == IndiState.Ok;
    public bool IsBusy => State == IndiState.Busy;
    public bool IsAlert => State == IndiState.Alert;
    public bool IsWritable => Permissions.Contains('w');
    public string Rule { get; set; } = string.Empty;
    public string Timeout { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Comment { get; set; } = string.Empty;

    public override string IndiTypeName => "Vector";

    internal abstract IndiVector CreateNewVector(params IndiValue[] items);
}

public class IndiVector<T> : IndiVector, IList<T>
    where T : IndiValue
{
    public int Count => _values.Count;

    private readonly List<T> _values = new();

    public T this[int index]
    {
        get => _values[index];
        set
        {
            if (!IsWritable) throw new InvalidOperationException("Vector is not writable");
            _values[index] = value;
        }
    }

    public override bool TryUpdateValue(IndiValue? value)
    {
        if (value is IndiVector<T> vec)
        {
            State = vec.State;
            Timeout = vec.Timeout;
            Timestamp = vec.Timestamp;
            Comment = vec.Comment;
            
            var newValues = new List<T>(_values.Count);
            foreach (var update in vec._values)
            {
                var existingProperty = _values.FirstOrDefault(x => x.Name == update.Name);
                if (existingProperty?.TryUpdateValue(update) ?? false)
                {
                    newValues.Add(existingProperty);
                }
                else
                {
                    newValues.Add(update);
                }
            }
            
            _values.Clear();
            _values.AddRange(newValues);
            return true;
        }
        
        return false;
    }

    public override bool TryUpdateValue(object value)
    {
        return TryUpdateValue(value as IndiValue);
    }

    internal override IndiVector CreateNewVector(params IndiValue[] items)
    {
        var vector = new IndiVector<T>(Name);
        foreach (var item in items)
            vector.Add((T)item);
        return vector;
    }

    internal override XElement CreateElement(string prefix, string subPrefix)
    {
        string innerIndiTypeName = _values.FirstOrDefault()?.IndiTypeName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(innerIndiTypeName))
            throw new ArgumentException("A vector must have an inner value");

        // Client -> Device messages require a subset of attributes.
        var isNewVector = prefix == "new";
        
        var element = new XElement(prefix + innerIndiTypeName + "Vector");
        
        if (!string.IsNullOrWhiteSpace(Name))
            element.Add(new XAttribute("name", Name));
        
        if (!isNewVector && !string.IsNullOrWhiteSpace(Label))
            element.Add(new XAttribute("label", Label));
        
        if (!isNewVector && !string.IsNullOrWhiteSpace(Group))
            element.Add(new XAttribute("group", Group));
        
        if (!isNewVector)
            element.Add(new XAttribute("state", State.ToString()));
       
        if (!isNewVector && !string.IsNullOrWhiteSpace(Permissions))
            element.Add(new XAttribute("perm", Permissions));
        
        if (!isNewVector && !string.IsNullOrWhiteSpace(Rule))
            element.Add(new XAttribute("rule", Rule));

        foreach (var item in _values)
            element.Add(item.CreateElement(subPrefix, subPrefix));

        return element;
    }

    public IndiVector() {}

    public IndiVector(string name)
    {
        Name = name;
    }

    public T? GetItemWithName(string name) => _values.FirstOrDefault(x => x.Name == name);

    public bool TryGetItemWithName(string name, out T value)
    {
        value = default!;
        
        var item = GetItemWithName(name);
        if (item is not null)
        {
            value = item;
            return true;
        }

        return false;
    }

    public void Add(T item) => _values.Add(item);

    public void AddRange(IEnumerable<T> items) => _values.AddRange(items);

    public void Clear() => _values.Clear();

    public bool Contains(T item) => _values.Contains(item);

    public void CopyTo(T[] array, int index) => _values.CopyTo(array, index);

    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

    public int IndexOf(T item) => _values.IndexOf(item);

    public void Insert(int index, T item) => _values.Insert(index, item);

    public bool Remove(T item) => _values.Remove(item);

    public void RemoveAt(int index) => _values.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}

public static class IndiVectorExtensions
{
    public static IndiSwitch? GetFirstEnabledSwitchOrNull(this IndiVector<IndiSwitch> options)
    {
        return options.FirstOrDefault(option => option.Value);
    }

    public static IEnumerable<IndiSwitch> GetEnabledSwitches(this IndiVector<IndiSwitch> options)
    {
        return options.Where(x => x.Value);
    }

    public static IndiSwitch? GetSwitchOrNull(this IndiVector<IndiSwitch> options, string name)
    {
        return options.FirstOrDefault(x => x.Name == name);
    }

    public static bool IsOn(this IndiVector<IndiSwitch> options, string name)
    {
        return options.GetSwitchOrNull(name)?.IsOn ?? false;
    }

    public static void SwitchTo(this IndiVector<IndiSwitch> options, string name)
    {
        foreach (var option in options)
            option.Value = option.Name == name;
    }

    public static void SwitchTo(this IndiVector<IndiSwitch> options, int option)
    {
        for (int i = 0; i < options.Count; i++)
            options[i].Value = i == option;
    }

    public static void SwitchTo(this IndiVector<IndiSwitch> options, Func<IndiSwitch, bool> selector)
    {
        foreach (var option in options)
            option.Value = selector(option);
    }
}