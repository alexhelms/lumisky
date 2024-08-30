using System.Xml.Linq;
using OdinEye.INDI.Primitives;

namespace OdinEye.INDI.Protocol;

public class IndiSetPropertyMessage : IIndiServerMessage
{
    public string DeviceName { get; }
    public string PropertyName { get; }
    public IndiVector PropertyValue { get; }

    public IndiSetPropertyMessage(string property, IndiVector value)
        : this(string.Empty, property, value)
    {
    }

    public IndiSetPropertyMessage(string device, string property, IndiVector value)
    {
        DeviceName = device;
        PropertyName = property;
        PropertyValue = value;
    }

    public XElement ToXml() => PropertyValue.CreateSetElement();

    public void Process(IndiConnection connection)
    {
        /*
         From INDI spec:
         if receive <setXXX> from Device
            change record of value and/or state for the specified Property
        */

        if (string.IsNullOrWhiteSpace(DeviceName))
        {
            foreach (var (_, device) in connection.Devices)
                UpdateProperty(device, PropertyName, PropertyValue);
        }
        else
        {
            var device = connection.Devices.GetDeviceOrNull(DeviceName);
            if (device is not null)
                UpdateProperty(device, PropertyName, PropertyValue);
        }
    }

    private void UpdateProperty(IndiDevice device, string property, IndiValue value)
    {
        if (device.Properties.Exists(property))
        {
            var prop = device.Properties.Get(property);
            prop.TryUpdateValue(value);
        }
        else
        {
            // Do nothing, SET does not make new properties.
        }
    }
}

public class IndiDefPropertyMessage : IIndiServerMessage
{
    public string DeviceName { get; }
    public string PropertyName { get; }
    public IndiVector PropertyValue { get; }
    
    public IndiDefPropertyMessage(string device, string property, IndiVector value)
    {
        DeviceName = device;
        PropertyName = property;
        PropertyValue = value;
    }
    
    public XElement ToXml() => PropertyValue.CreateDefElement();
    
    public void Process(IndiConnection connection)
    {
        /*
         From INDI spec:
         if receive <defProperty> from Device
             if first time to see this device=
                 create new Device record
             if first time to see this device+name combination
                 create new Property record within given Device
        */

        connection.CreateOrConfigureDevice(
            DeviceName,
            device =>
            {
                device.Properties[PropertyName] = PropertyValue;
            });
    }
}

public class IndiDeletePropertyMessage : IIndiServerMessage
{
    public string DeviceName { get; }
    public string PropertyName { get; }
    public DateTime Timestamp { get; }
    public string Message { get; }
    
    public IndiDeletePropertyMessage(
        string device, 
        string property,
        DateTime? timestamp = null,
        string? message = null)
    {
        DeviceName = device;
        PropertyName = property;
        Timestamp = timestamp.GetValueOrDefault();
        Message = message ?? string.Empty;
    }

    public XElement ToXml()
    {
        var element = new XElement("delProperty");
        
        if (!string.IsNullOrWhiteSpace(DeviceName))
            element.Add(new XAttribute("device", DeviceName));
        
        if (!string.IsNullOrWhiteSpace(PropertyName))
            element.Add(new XAttribute("name", PropertyName));
        
        if (Timestamp != DateTime.MinValue)
            element.Add(new XAttribute("timestamp", Timestamp.ToString("O")));
        
        if (!string.IsNullOrWhiteSpace(Message))
            element.Add(new XAttribute("message", Message));

        return element;
    }

    public void Process(IndiConnection connection)
    {
        /*
         From INDI spec:
         if receive <delProperty> from Device
             if includes device= attribute
                 if includes name= attribute
                     delete record for just the given Device+name
                 else
                     delete all records the given Device
             else
                 delete all records for all devices 
        */

        if (!string.IsNullOrWhiteSpace(DeviceName))
        {
            var device = connection.Devices.GetDeviceOrThrow(DeviceName);
            
            if (!string.IsNullOrWhiteSpace(PropertyName))
            {
                device.Properties.Delete(PropertyName);
            }
            else
            {
                connection.RemoveDevice(DeviceName);
            }
        }
        else
        {
            connection.RemoveAllDevices();
        }
    }
}

public class IndiNotificationMessage : IIndiServerMessage
{
    public string DeviceName { get; }
    public DateTime Timestamp { get; }
    public string Message { get; }

    public IndiNotificationMessage(
        string device,
        DateTime? timestamp = null,
        string? message = null)
    {
        DeviceName = device;
        Timestamp = timestamp.GetValueOrDefault();
        Message = message ?? string.Empty;
    }
    
    public XElement ToXml()
    {
        var element = new XElement("message");
        
        if (!string.IsNullOrWhiteSpace(DeviceName))
            element.Add(new XAttribute("device", DeviceName));
        
        if (Timestamp != DateTime.MinValue)
            element.Add(new XAttribute("timestamp", Timestamp.ToString("O")));
        
        if (!string.IsNullOrWhiteSpace(Message))
            element.Add(new XAttribute("message", Message));

        return element;
    }

    public void Process(IndiConnection connection)
    {
        // nothing to do, perhaps log?
    }
}