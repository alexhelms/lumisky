using System.Xml.Linq;

namespace OdinEye.INDI.Primitives;

public abstract class IndiValue
{
    public string Name { get; set; } = string.Empty;
    
    public string? Label { get; set; }
    
    public abstract string IndiTypeName { get; }
    public abstract bool TryUpdateValue(IndiValue value);
    public abstract bool TryUpdateValue(object value);
    internal abstract XElement CreateElement(string prefix, string subPrefix);

    public XElement CreateNewElement() => CreateElement("new", "one");

    public XElement CreateSetElement() => CreateElement("set", "one");

    public XElement CreateDefElement() => CreateElement("def", "def");
}

public abstract class IndiValue<T> : IndiValue
{
    public T? Value { get; set; }

    protected IndiValue() {}

    protected IndiValue(string name)
    {
        Name = name;
    }

    protected IndiValue(string name, string label)
        : this(name)
    {
        Label = label;
    }

    protected IndiValue(string name, T? value)
        : this(name)
    {
        Value = value;
    }

    protected IndiValue(string name, string label, T? value)
        : this(name, label)
    {
        Value = value;
    }

    public override bool TryUpdateValue(IndiValue? value)
    {
        if (value is IndiValue<T> specific)
        {
            UpdateValue(specific);
            return true;
        }

        return false;
    }

    public override bool TryUpdateValue(object value)
    {
        if (value is T t)
        {
            Value = t;
            return true;
        }

        try
        {
            Value = (T)Convert.ChangeType(value, typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void UpdateValue(IndiValue<T> value)
    {
        Value = value.Value;
    }
}
