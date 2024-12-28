using System.Xml.Linq;

namespace LumiSky.INDI.Primitives;

public class IndiLight : IndiValue<IndiState>
{
    public override string IndiTypeName => "Light";
    
    public IndiLight(string name)
    {
        Name = name;
    }

    public IndiLight(string name, IndiState value)
        : this(name)
    {
        Value = value;
    }

    public IndiLight(string name, string label, IndiState value)
        : this(name, value)
    {
        Label = label;
    }

    internal override XElement CreateElement(string prefix, string subPrefix)
    {
        var element = new XElement(prefix + IndiTypeName, new XText(Value.ToString()));
        
        // Client -> Device messages require a subset of attributes.
        var isNewVector = prefix == "new";
        
        if (!string.IsNullOrWhiteSpace(Name))
            element.Add(new XAttribute("name", Name));
        
        if (!isNewVector && !string.IsNullOrWhiteSpace(Label))
            element.Add(new XAttribute("label", Label));

        return element;
    }
}