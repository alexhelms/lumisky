using System.Xml.Linq;

namespace LumiSky.INDI.Primitives;

public class IndiBlob : IndiValue<byte[]>
{
    public override string IndiTypeName => "BLOB";

    public string Format { get; init; } = ".bin";

    public IndiBlob()
    {
        Value = Array.Empty<byte>();
    }

    public IndiBlob(string name)
        : this()
    {
        Name = name;
    }

    public IndiBlob(string name, string label)
        : this(name)
    {
        Label = label;
    }
    
    internal override XElement CreateElement(string prefix, string subPrefix)
    {
        throw new NotImplementedException();
    }
}