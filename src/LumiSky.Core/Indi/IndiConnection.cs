using LumiSky.Core.Indi.Serialization;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Xml;
using System.Xml.Serialization;

namespace LumiSky.Core.Indi;

public class IndiConnection : IDisposable
{
    private TcpClient? _tcpClient;
    private Channel<string>? _xmlChannel;
    private CancellationTokenSource? _channelCts;
    private bool _isDisconnecting;

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Disconnect();
    }

    public async Task<ChannelReader<IIndiCommand>> Connect(
        string hostname,
        int port = 7624,
        CancellationToken token = default)
    {
        if (IsConnected) throw new AlreadyConnectedException();

        try
        {
            _tcpClient = new TcpClient
            {
                SendTimeout = 1000,
                NoDelay = true
            };

            await _tcpClient.ConnectAsync(hostname, port, token);

            _xmlChannel = Channel.CreateUnbounded<string>();
            var cmdChannel = Channel.CreateUnbounded<IIndiCommand>();
            var networkStream = _tcpClient.GetStream();

            _ = Task.Factory.StartNew(WriteThread, (networkStream, _xmlChannel.Reader), TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            _ = Task.Factory.StartNew(ReadThread, (networkStream, cmdChannel.Writer), TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

            return cmdChannel.Reader;
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionRefused)
        {
            Log.Error("Failed to connect to INDI server at {Hostname}:{Port}, server did not respond", hostname, port);
            Disconnect();
            throw new NotConnectedException("Failed to connect to INDI server", se);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unhandled INDI exception connecting to {Hostname}:{Port}", hostname, port);
            Disconnect();
            throw new NotConnectedException("Unhandled INDI exception", e);
        }
    }

    public void Disconnect()
    {
        try
        {
            _isDisconnecting = true;

            _channelCts?.Cancel();

            _tcpClient?.Close();
            _tcpClient = null;
        }
        finally
        {
            _isDisconnecting = false;
        }
    }

    public async Task SendCommand(IIndiCommand command)
    {
        if (IsConnected && _xmlChannel is not null)
        {
            var commandType = command.GetType();
            var serializer = GetSerializer(commandType);

            var stringBuilder = new StringBuilder(256);
            using var xmlWriter = XmlWriter.Create(stringBuilder, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
            });

            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            serializer.Serialize(xmlWriter, command, ns);

            var xml = stringBuilder.ToString();

            await _xmlChannel.Writer.WriteAsync(xml.ReplaceLineEndings("\n"));
        }
    }

    public Task RefreshProperties(string? deviceName = null, string? propertyName = null)
    {
        return SendCommand(new GetProperties
        {
            Device = deviceName,
            Name = propertyName,
        });
    }

    public Task EnableBlobs(string device, BlobEnable state = BlobEnable.Also, string? propertyName = null)
    {
        return SendCommand(new EnableBlob
        {
            Device = device,
            Name = propertyName,
            State = state,
        });
    }

    private async Task WriteThread(object? state)
    {
        if (state is not (Stream stream, ChannelReader<string> xmlChannelReader))
            throw new ArgumentNullException(nameof(state));

        _channelCts = new CancellationTokenSource();

        try
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await foreach (var item in xmlChannelReader.ReadAllAsync(_channelCts.Token))
            {
                await writer.WriteAsync(item);  
                await writer.FlushAsync(_channelCts.Token);
            }
        }
        catch (ObjectDisposedException)
        {
            // Connection was closed and stream was disposed.
        }
        catch (IOException e) when (e.InnerException is SocketException se)
        {
            // When reader.Read() is waiting on data and the TcpClient is disposed, we can ignore this error.
            if (se.SocketErrorCode == SocketError.ConnectionAborted)
                return;

            Log.Error(e, "IOException in INDI write thread");
            Disconnect();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log.Error(e, "Exception in INDI write thread");
            Disconnect();
        }
        finally
        {
            _channelCts.Dispose();
            _channelCts = null;
        }
    }

    private static readonly Lock SerializerLock = new();
    private static Dictionary<string, XmlSerializer> SerializerCache = [];

    // XmlSerializer has to be told of all types it is allowed to serialize/deserialize.
    // When using XmlSerializer, we explicitly say the root element type, but it needs
    // to know all other allowed types that could be children elements.
    private static readonly Type[] SerializerExtraTypes = [
        typeof(DefText),
        typeof(DefNumber),
        typeof(DefSwitch),
        typeof(DefLight),
        typeof(DefBlob),
        typeof(OneText),
        typeof(OneNumber),
        typeof(OneSwitch),
        typeof(OneLight),
        typeof(OneBlob),
    ];

    private static XmlSerializer GetSerializer(Type type, string? rootName = null)
    {
        lock (SerializerLock)
        {
            var cacheKey = type.Name;

            if (!SerializerCache.TryGetValue(cacheKey, out var serializer))
            {
                var rootAttribute = string.IsNullOrWhiteSpace(rootName) ? null : new XmlRootAttribute(rootName);

                serializer = new XmlSerializer(type,
                    overrides: null,
                    extraTypes: SerializerExtraTypes,
                    root: rootAttribute,
                    defaultNamespace: string.Empty);

                // Cache the serializers.
                // A dynamic assembly is created each time and can take several milliseconds.
                SerializerCache.Add(cacheKey, serializer);
            }

            return serializer;
        }
    }

    private async Task ReadThread(object? state)
    {
        if (state is not (Stream stream, ChannelWriter<IIndiCommand> cmdChannelWriter))
            throw new ArgumentNullException(nameof(state));

        var xmlSettings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            ConformanceLevel = ConformanceLevel.Fragment,
        };

        static T? DeserializeElement<T>(XmlReader reader) where T : class
        {
            var rootName = reader.Name;
            var serializer = GetSerializer(typeof(T), rootName);
            return serializer.Deserialize(reader.ReadSubtree()) as T;
        }
        
        using var textReader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        using var reader = XmlReader.Create(textReader, xmlSettings);
        
        do
        {
            try
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        IIndiCommand? command = reader.Name switch
                        {
                            "defTextVector" => DeserializeElement<DefTextVector>(reader),
                            "defNumberVector" => DeserializeElement<DefNumberVector>(reader),
                            "defSwitchVector" => DeserializeElement<DefSwitchVector>(reader),
                            "defLightVector" => DeserializeElement<DefLightVector>(reader),
                            "defBLOBVector" => DeserializeElement<DefBlobVector>(reader), //
                            "setTextVector" => DeserializeElement<SetTextVector>(reader),
                            "setNumberVector" => DeserializeElement<SetNumberVector>(reader),
                            "setSwitchVector" => DeserializeElement<SetSwitchVector>(reader),
                            "setLightVector" => DeserializeElement<SetLightVector>(reader),
                            "setBLOBVector" => DeserializeElement<SetBlobVector>(reader), //
                            "newTextVector" => DeserializeElement<NewTextVector>(reader),
                            "newNumberVector" => DeserializeElement<NewNumberVector>(reader),
                            "newSwitchVector" => DeserializeElement<NewSwitchVector>(reader),
                            "newBLOBVector" => DeserializeElement<NewBlobVector>(reader),
                            "delProperty" => DeserializeElement<DelProperty>(reader),
                            "message" => DeserializeElement<Message>(reader),
                            _ => null,
                        };

                        if (command is not null)
                        {
                            await cmdChannelWriter.WriteAsync(command);
                        }
                    }
                }

                if (reader.EOF)
                {
                    if (!_isDisconnecting)
                    {
                        Disconnect();
                        //OnConnectionLost();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // XmlReader blocks on Read(), when the connection is closed it tries
                // to read but the stream is disposed.
            }
            catch (IOException e) when (e.InnerException is SocketException se)
            {
                // When reader.Read() is waiting on data and the TcpClient is disposed, we can ignore this error.
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                    return;

                Log.Error(e, "IOException in INDI read thread");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception in INDI read thread");
                Disconnect();
            }
        } while (IsConnected);
    }
}
