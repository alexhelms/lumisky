using System.Xml.Linq;
using OdinEye.INDI.Primitives;

namespace OdinEye.INDI.Protocol;

public class IndiGetPropertiesMessage : IIndiClientMessage
{
    public string DeviceName { get; }
    public string PropertyName { get; }
    public string Version { get; } = "1.7";

    public IndiGetPropertiesMessage()
    {
        DeviceName = string.Empty;
        PropertyName = string.Empty;
    }

    public IndiGetPropertiesMessage(string device)
    {
        DeviceName = device;
        PropertyName = string.Empty;
    }
    
    public IndiGetPropertiesMessage(string device, string property)
        : this(device)
    {
        PropertyName = property;
    }

    public XElement ToXml()
    {
        var element = new XElement("getProperties",
            new XAttribute("version", Version));

        if (!string.IsNullOrWhiteSpace(DeviceName))
            element.Add(new XAttribute("device", DeviceName));
        
        if (!string.IsNullOrWhiteSpace(PropertyName))
            element.Add(new XAttribute("name", PropertyName));

        return element;
    }
}

public class IndiNewPropertyMessage : IIndiClientMessage
{
    public string DeviceName { get; }
    public string PropertyName { get; }
    public IndiValue PropertyValue { get; }

    public IndiNewPropertyMessage(string name, string property, IndiValue value)
    {
        DeviceName = name;
        PropertyName = property;
        PropertyValue = value;
    }
    
    public XElement ToXml()
    {
        ArgumentNullException.ThrowIfNull(PropertyValue, nameof(PropertyValue));
        
        var element = PropertyValue.CreateNewElement();
        element.AddOrUpdateAttribute("device", DeviceName);
        element.AddOrUpdateAttribute("name", PropertyName);
        return element;
    }
}

public class IndiEnableBlobMessage : IIndiClientMessage
{
    public string DeviceName { get; }
    public string PropertyName { get; }
    public IndiBlobState State { get; }

    public IndiEnableBlobMessage(
        string deviceName,
        IndiBlobState blobState,
        string? propertyName = null)
    {
        DeviceName = deviceName;
        PropertyName = propertyName ?? string.Empty;
        State = blobState;
    }

    public XElement ToXml()
    {
        var element = new XElement("enableBLOB", new XText(State.ToString()));
        
        if (!string.IsNullOrWhiteSpace(DeviceName))
            element.Add(new XAttribute("device", DeviceName));
        
        if (!string.IsNullOrWhiteSpace(PropertyName))
            element.Add(new XAttribute("name", PropertyName));

        return element;
    }
}