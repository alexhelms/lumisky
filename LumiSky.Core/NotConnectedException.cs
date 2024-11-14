namespace LumiSky.Core;

public class NotConnectedException : Exception
{
    public NotConnectedException()
        : this("Device not connected.")
    {
    }

    public NotConnectedException(string? message) : base(message)
    {
    }
}
