namespace OdinEye.Core;

public class NotConnectedException : Exception
{
    public NotConnectedException()
    {
    }

    public NotConnectedException(string? message) : base(message)
    {
    }
}
