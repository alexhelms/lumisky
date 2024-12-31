using System.Diagnostics;
using LumiSky.INDI.Primitives;

namespace LumiSky.INDI.Protocol;

public class IndiClient : IDisposable
{
    public const int DefaultPort = 7624;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; }
    public bool IsConnected => Connection?.IsConnected ?? false;
    
    public IndiConnection? Connection { get; private set; }

    public IndiClient() { }
    
    public IndiClient(string host, int port = DefaultPort)
    {
        Host = host;
        Port = port;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Disconnect();
    }

    public async Task<bool> Connect()
    {
        if (IsConnected)
            return true;
        
        var connection = new IndiConnection();

        var sw = Stopwatch.StartNew();
        connection.DefinePropertyReceived += ConnectionOnDefinePropertyReceived;

        try
        {
            await connection.Connect(Host, Port);
            await connection.QueryProperties();

            // Wait until the line goes silent for a brief time so we know we got everything.
            while (connection.IsConnected && sw.Elapsed < TimeSpan.FromMilliseconds(10))
                await Task.Delay(1);
        }
        finally
        {
            connection.DefinePropertyReceived -= ConnectionOnDefinePropertyReceived;
        }

        if (connection.IsConnected)
        {
            Connection = connection;
            return true;
        }
        
        connection.Disconnect();
        return false;
        
        void ConnectionOnDefinePropertyReceived(IndiDevice device, string property, IndiVector value)
        {
            sw.Restart();
        }
    }

    public void Disconnect()
    {
        Connection?.Dispose();
        Connection = null;
    }
}