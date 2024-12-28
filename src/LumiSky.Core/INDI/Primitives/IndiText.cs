using System.Xml.Linq;

namespace LumiSky.INDI.Primitives;

public class IndiText : IndiValue<string>
{
    public override string IndiTypeName => "Text";
	
    public IndiText(string name)
    {
        Name = name;
    }

    public IndiText(string name, string? value)
        : this(name)
    {
        Value = value;
    }

    public IndiText(string name, string label, string? value)
        : this(name, value)
    {
        Label = label;
    }

    internal override XElement CreateElement(string prefix, string subPrefix)
    {
        var element = new XElement(prefix + IndiTypeName, new XText(Value ?? string.Empty));
        
        // Client -> Device messages require a subset of attributes.
        var isNewVector = prefix == "new";
        
        if (!string.IsNullOrWhiteSpace(Name))
            element.Add(new XAttribute("name", Name));
        
        if (!isNewVector && !string.IsNullOrWhiteSpace(Label))
            element.Add(new XAttribute("label", Label));

        return element;
    }
}