using System.Xml.Linq;

namespace OdinEye.INDI.Primitives;

public class IndiSwitch : IndiValue<bool>
{
    public bool IsOn => Value;
    public override string IndiTypeName => "Switch";
    
    public IndiSwitch() {}

    public IndiSwitch(string name)
    {
        Name = name;
    }

    public IndiSwitch(string name, bool value)
        : this(name)
    {
        Value = value;
    }

    public IndiSwitch(string name, string label, bool value)
        : this(name, value)
    {
        Label = label;
    }

    internal override XElement CreateElement(string prefix, string subPrefix)
    {
        var element = new XElement(
            prefix + IndiTypeName,
            new XText(IsOn ? "On" : "Off"));
        
        // Client -> Device messages require a subset of attributes.
        var isNewVector = prefix == "new";
        
        if (!string.IsNullOrWhiteSpace(Name))
            element.Add(new XAttribute("name", Name));

        if (!isNewVector && !string.IsNullOrWhiteSpace(Label))
            element.Add(new XAttribute("label", Label));

        return element;
    }
}