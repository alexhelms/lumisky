using System.Collections.Concurrent;

namespace OdinEye.INDI.Protocol;

public class DeviceCollection : ConcurrentDictionary<string, IndiDevice>
{
    public IEnumerable<IndiDevice> AllDevices => Values;

    public IEnumerable<IndiDevice> ConnectedDevices
        => AllDevices.Where(x => x.IsConnected);

    public IEnumerable<IndiDevice> DisconnectedDevices
        => AllDevices.Where(x => !x.IsConnected);

    public IEnumerable<IndiDevice> Cameras
        => AllDevices.Where(x => x.Properties.Exists("CCD_INFO"));

    public IndiDevice? GetDeviceOrNull(string name)
    {
        return TryGetValue(name, out var device) ? device : null;
    }
    
    public IndiDevice GetDeviceOrThrow(string name)
    {
        if (TryGetValue(name, out var device))
            return device;
        throw new ArgumentException($"Device \"{name}\" not found on this connection");
    }
}