using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OdinEye.INDI.Primitives;

public class IndiNumber : IndiValue<double>
{
    public string Format { get; set; } = "%f";
    public double Min { get; set; }
    public double Max { get; set; }
    public double Step { get; set; }
    public override string IndiTypeName => "Number";
    
    public IndiNumber() {}

    public IndiNumber(string name)
    {
        Name = name;
    }

    public IndiNumber(string name, double value)
        : this(name)
    {
        Value = value;
    }

    public IndiNumber(string name, string label, double value)
        : this(name, value)
    {
        Label = label;
    }
    
    internal override XElement CreateElement(string prefix, string subPrefix)
    {
        // Client -> Device messages require a subset of attributes.
        var isNewVector = prefix == "new";
        
        var element = new XElement(prefix + IndiTypeName);

        if (!isNewVector)
        {
            element.Add(
                new XAttribute("format", Format),
                new XAttribute("min", Min.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("max", Max.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("step", Step.ToString(CultureInfo.InvariantCulture))    
            );
        }

        if (!string.IsNullOrWhiteSpace(Name))
            element.Add(new XAttribute("name", Name));

        if (!isNewVector && !string.IsNullOrWhiteSpace(Label))
            element.Add(new XAttribute("label", Label));

        element.Add(new XText(Value.ToString(CultureInfo.InvariantCulture)));

        return element;
    }
}