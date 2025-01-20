namespace LumiSky.Core.Indi;

public class NotConnectedException : Exception
{
    public NotConnectedException()
    {
    }

    public NotConnectedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
