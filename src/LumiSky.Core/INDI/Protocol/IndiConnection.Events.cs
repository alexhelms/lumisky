using LumiSky.INDI.Primitives;

namespace LumiSky.INDI.Protocol;

public partial class IndiConnection
{
    public delegate void ConnectedHandler();
    public delegate void ConnectionLostHandler();
    public delegate void DisconnectedHandler();
    public delegate void MessageSentHandler(IIndiClientMessage message);
    public delegate void MessageReceivedHandler(IIndiServerMessage message);
    public delegate void DefinePropertyHandler(IndiDevice device, string property, IndiVector value);
    public delegate void DeletePropertyHandler(IndiDevice device, string property, IndiVector value);
    public delegate void SetPropertyHandler(IndiDevice device, string property, IndiVector value);
    public delegate void NotificationReceivedHandler(IndiDevice device, string message);
    public delegate void DeviceFoundHandler(IndiDevice device);
    public delegate void DeviceRemovedHandler(IndiDevice device);

    public event ConnectedHandler? Connected;
    public event ConnectionLostHandler? ConnectionLost;
    public event DisconnectedHandler? Disconnected;
    public event MessageSentHandler? MessageSent;
    public event MessageReceivedHandler? MessageReceived;
    public event DefinePropertyHandler? DefinePropertyReceived;
    public event DeletePropertyHandler? DeletePropertyReceived;
    public event SetPropertyHandler? SetPropertyReceived;
    public event NotificationReceivedHandler? NotificationReceived;
    public event DeviceFoundHandler? DeviceFound;
    public event DeviceRemovedHandler? DeviceRemoved;

    private void OnConnected()
        => Connected?.Invoke();

    public void OnConnectionLost()
        => ConnectionLost?.Invoke();
    
    private void OnDisconnected()
        => Disconnected?.Invoke();

    private void OnMessageSent(IIndiClientMessage message)
        => MessageSent?.Invoke(message);

    private void OnMessageReceived(IIndiServerMessage message)
        => MessageReceived?.Invoke(message);

    private void OnDefinePropertyReceived(IndiDevice device, string property, IndiVector value)
        => DefinePropertyReceived?.Invoke(device, property, value);

    private void OnDeletePropertyReceived(IndiDevice device, string property, IndiVector value)
        => DeletePropertyReceived?.Invoke(device, property, value);

    private void OnSetPropertyReceived(IndiDevice device, string property, IndiVector value)
        => SetPropertyReceived?.Invoke(device, property, value);

    private void OnNotificationReceived(IndiDevice device, string message)
        => NotificationReceived?.Invoke(device, message);

    private void OnDeviceFound(IndiDevice device)
        => DeviceFound?.Invoke(device);
    
    private void OnDeviceRemoved(IndiDevice device)
        => DeviceRemoved?.Invoke(device);
}