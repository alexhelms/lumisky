using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml;
using OdinEye.INDI.Primitives;

namespace OdinEye.INDI.Protocol;

public partial class IndiConnection
{
    private TcpClient? _client;
    private Channel<string>? _indiChannel;
    private string? _hostname;
    private int _port;
    private bool _disconnecting;
    private bool _attemptReconnect = true;
    private List<string> _knownConnectedDevices = new();
    
    public DeviceCollection Devices { get; } = new();

    public bool IsConnected => _client is not null && _client.Connected;

    public IndiConnection()
    {
        ConnectionLost += IndiConnection_ConnectionLost;
    }

    ~IndiConnection()
    {
        _attemptReconnect = false;
        Disconnect();
    }

    private async void IndiConnection_ConnectionLost()
    {
        Log.Warning("INDI connection at {Hostname}:{Port} lost, attempting to reconnect", _hostname, _port);

        while (!IsConnected && _attemptReconnect)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                await Connect(_hostname!, _port);
            }
            catch (Exception)
            {
            }

            if (IsConnected)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                // Reconnect to each device that was previously connected.
                foreach (var deviceName in _knownConnectedDevices)
                {
                    if (Devices.GetDeviceOrNull(deviceName) is { } device)
                    {
                        for (int i = 2; i > -1; i--)
                        {
                            try
                            {
                                await device.Connect();
                            }
                            catch (Exception)
                            {
                                // Sometimes sending CONNECTION property can time out on first try
                            }

                            if (device.IsConnected)
                                break;
                        }
                    }
                }

                _knownConnectedDevices.Clear();
                Log.Warning("Reconnected to INDI at {Hostname!}:{Port}", _hostname, _port);
            }
        }
    }

    public async Task Connect(string hostname, int port = 7624)
    {
        if (IsConnected) return;
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            _hostname = hostname;
            _port = port;
            _client = new TcpClient();
            _client.Client.SendTimeout = 10000;
            _client.Client.NoDelay = true;
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 5);
            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 5);

            await _client.ConnectAsync(hostname, port, cts.Token);

            if (IsConnected)
            {
                _indiChannel = Channel.CreateUnbounded<string>();
                var networkStream = _client.GetStream();

                _ = Task.Factory.StartNew(WriteThread, networkStream,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

                _ = Task.Factory.StartNew(ReadThread, networkStream,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

                await QueryProperties();
                OnConnected();
            }
        }
        catch (OperationCanceledException oce)
        {
            if (cts.IsCancellationRequested)
            {
                var timeoutException = new TimeoutException($"Timed out connecting to INDI server {hostname}:{port}");
                Log.Error(timeoutException, "Timed out connecting to INDI server {Hostname}:{Port}", hostname, port);
                throw timeoutException;
            }
            else
            {
                Log.Error(oce.GetBaseException(), "Error connecting to {Hostname}:{Port}", hostname, port);
            }
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionRefused)
        {
            Log.Error("Failed to connect to INDI server at {Hostname}:{Port}, server did not respond", hostname, port);
            Disconnect();
        }
        catch (Exception e)
        {
            Log.Error(e, "Unhandled INDI exception connecting to {Hostname}:{Port}", hostname, port);
            Disconnect();
        }
    }

    public void Disconnect()
    {
        try
        {
            _disconnecting = true;

            _client?.Close();
            _client = null;

            OnDisconnected();
        }
        finally
        {
            _disconnecting = false;
        }
    }

    internal void CreateOrConfigureDevice(string name, Action<IndiDevice> config)
    {
        var originalDeviceCount = Devices.Count;
        var device = Devices.GetOrAdd(name, new IndiDevice(name, this));
        config(device);
        if (Devices.Count > originalDeviceCount)
            OnDeviceFound(device);
    }

    internal void RemoveDevice(string name)
    {
        if (Devices.Remove(name, out var device))
            OnDeviceRemoved(device);
    }

    internal void RemoveAllDevices()
    {
        var deviceNames = Devices.AllDevices.Select(x => x.Name).ToList();
        foreach (var deviceName in deviceNames)
        {
            RemoveDevice(deviceName);
        }
    }

    public Task QueryProperties()
    {
        return Send(new IndiGetPropertiesMessage());
    }

    public async Task Send(IIndiClientMessage message)
    {
        if (IsConnected && _indiChannel is not null)
        {
            var xml = message.ToXml();
            var str = xml.ToString().ReplaceLineEndings();
            await _indiChannel.Writer.WriteAsync(str);
            OnMessageSent(message);
        }
    }

    private async Task WriteThread(object? state)
    {
        var stream = state as NetworkStream;
        if (stream is null)
            throw new ArgumentNullException(nameof(state));
        if (_indiChannel is null)
            throw new NullReferenceException(nameof(_indiChannel));
        
        try
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await foreach (var item in _indiChannel.Reader.ReadAllAsync())
            {
                await writer.WriteAsync(item);
                await writer.FlushAsync();
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
        catch (Exception e)
        {
            Log.Error(e, "Exception in INDI write thread");
            Disconnect();
        }
        finally
        {
            _indiChannel = null;
        }
    }

    private void ReadThread(object? state)
    {
        var stream = state as NetworkStream;
        if (stream is null)
            throw new ArgumentNullException(nameof(state));
        
        var xmlSettings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            XmlResolver = null,
            CloseInput = false,
        };
        
        while (IsConnected)
        {
            try
            {
                // XmlReader is being used to parse the stream without loading it entirely
                // into memory. Almost all INDI xml elements are small enough to not matter,
                // but that assumption falls apart with blobs which could be hundreds of megabytes!
                using var textReader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                using var reader = XmlReader.Create(textReader, xmlSettings);

                while (reader.Read())
                {
                    IndiVector? vector = null;
                    var device = reader.GetAttribute("device") ?? string.Empty;
                    var name = reader.GetAttribute("name") ?? string.Empty;

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element when
                            reader.IsEmptyElement &&
                            reader.Name == "delProperty":
                            {
                                var timestamp = ParseDateTime(reader);
                                var comment = reader.GetAttribute("message") ?? string.Empty;
                                var message = new IndiDeletePropertyMessage(device, name, timestamp, comment);
                                ProcessMessage(message);
                                continue;
                            }
                        case XmlNodeType.Element when
                            reader.IsEmptyElement &&
                            reader.Name == "message":
                            {
                                var timestamp = ParseDateTime(reader);
                                var comment = reader.GetAttribute("message") ?? string.Empty;
                                var message = new IndiNotificationMessage(device, timestamp, comment);
                                ProcessMessage(message);
                                continue;
                            }
                        case XmlNodeType.Element when
                            reader.Name.EndsWith("Vector"):
                            vector = ParseVector(reader, name);
                            break;
                    }

                    if (vector is null) continue;

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element when
                            reader.Name == "setBLOBVector" &&
                            vector is IndiVector<IndiBlob> blobVector:
                            {
                                // Special case for blobs because they can be enormous
                                var indiBlob = ParseIndiBlob(reader, name);
                                blobVector.Add(indiBlob);
                                var message = new IndiSetPropertyMessage(device, name, vector);
                                ProcessMessage(message);
                                break;
                            }
                        case XmlNodeType.Element when
                            reader.Name.StartsWith("set"):
                            {
                                var message = ParseSetVector(reader, vector, device);
                                ProcessMessage(message);
                                break;
                            }
                        case XmlNodeType.Element when
                            reader.Name.StartsWith("def"):
                            {
                                var message = ParseDefVector(reader, vector, device);
                                ProcessMessage(message);
                                break;
                            }
                    }
                }

                if (reader.EOF)
                {
                    if (!_disconnecting)
                    {
                        // Capture the devices that were connected when connection was lost
                        _knownConnectedDevices.Clear();
                        _knownConnectedDevices.AddRange(
                            Devices.AllDevices
                                .Where(x => x.IsConnected)
                                .Select(x => x.Name)
                        );

                        Disconnect();
                        OnConnectionLost();
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
        }
    }

    private IndiVector? ParseVector(XmlReader reader, string name)
    {
        // Skip def/set/new
        IndiVector? vector = reader.Name[3..] switch
        {
            "SwitchVector" => new IndiVector<IndiSwitch>(),
            "NumberVector" => new IndiVector<IndiNumber>(),
            "TextVector" => new IndiVector<IndiText>(),
            "BLOBVector" => new IndiVector<IndiBlob>(),
            "LightVector" => new IndiVector<IndiLight>(),
            _ => null, 
        };

        if (vector is not null)
        {
            vector.Name = name;
            vector.Label = reader.GetAttribute("label") ?? vector.Name;
            vector.Group = reader.GetAttribute("group") ?? string.Empty;
            vector.Permissions = reader.GetAttribute("perm") ?? "ro";
            vector.Rule = reader.GetAttribute("rule") ?? string.Empty;
            vector.Timeout = reader.GetAttribute("timeout") ?? string.Empty;
            vector.Comment = reader.GetAttribute("message") ?? string.Empty;
                            
            IndiState state = IndiState.Idle;
            var stateStr = reader.GetAttribute("state");
            if (!string.IsNullOrWhiteSpace(stateStr))
                _ = Enum.TryParse(reader.GetAttribute("state"), true, out state);
            vector.State = state;
                            
            vector.Timestamp = ParseDateTime(reader);
        }

        return vector;
    }
    
    private DateTime ParseDateTime(XmlReader reader)
    {
        DateTime timestamp = DateTime.MinValue;
        var timestampStr = reader.GetAttribute("timestamp");
        if (!string.IsNullOrWhiteSpace(timestampStr))
            _ = DateTime.TryParse(reader.GetAttribute("timestamp"), out timestamp);
        return timestamp;
    }

    private IndiBlob ParseIndiBlob(XmlReader reader, string name)
    {
        // Read "oneBlob" Element
        reader.Read();
        
        var label = reader.GetAttribute("label") ?? name;

        // "size" is number of bytes in decoded and uncompressed blob
        // "len" is the number of bytes in the oneBlob message
        var size = int.Parse(reader.GetAttribute("size") ?? "0");
        var length = int.Parse(reader.GetAttribute("len") ?? size.ToString());
        
        // "format" varies, can be .bin, .z, .fits.z ...
        // TODO: do something with the format
        var format = reader.GetAttribute("format") ?? ".bin";
        
        byte[] base64Buffer = new byte[8192];  // TODO: Benchmarks to tune this buffer size for large images
        byte[] blobBuffer = new byte[length];  // TODO: more sophisticated memory management for large blobs

        // Move to the element contents
        reader.Read();

        int readBytes = 0, bytesOffset = 0;
        while ((readBytes = reader.ReadContentAsBase64(base64Buffer, 0, base64Buffer.Length)) > 0)
        {
            var srcSpan = base64Buffer.AsSpan()[..readBytes];
            var dstSpan = blobBuffer.AsSpan()[bytesOffset..];
            srcSpan.CopyTo(dstSpan);
            bytesOffset += readBytes;
        }
        
        // Read the EndElement of setBLOBVector
        reader.Read();
        
        if (reader.NodeType != XmlNodeType.EndElement ||
            !reader.Name.StartsWith("setBLOBVector"))
            throw new InvalidOperationException("Malformed XML");

        return new IndiBlob(name, label)
        {
            Format = format,
            Value = blobBuffer,
        };
    }
    
    private IndiSetPropertyMessage ParseSetVector(XmlReader reader, IndiVector vector, string device)
    {
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement &&
                reader.Name.StartsWith("set") &&
                reader.Name.EndsWith("Vector"))
                break;
            
            if (reader.NodeType == XmlNodeType.Element &&
                reader.Name.StartsWith("one"))
            {
                var name = reader.GetAttribute("name") ?? string.Empty;

                // Get the text
                reader.Read();

                var value = reader.Value.Trim();

                // EndElement
                reader.Read();

                // Note: Blobs are handled elsewhere as a special case.
                switch (vector)
                {
                    case IndiVector<IndiText> textVector:
                    {
                        textVector.Add(new IndiText(name, value));
                        break;
                    }
                    case IndiVector<IndiNumber> numberVector:
                    {
                        // The number format was sent in the defNumberVector.
                        // When we receive a setNumberVector, we have to lookup the format which much exist.
                        // If it doesnt exist, throw, because that is a state error.
                        var indiDevice = Devices.GetDeviceOrThrow(device);
                        if (!indiDevice.Properties.TryGet(vector.Name, out _))
                        {
                            Log.Debug("INDI Vector {VectorName} received but was not found in the property dictionary.", vector.Name);
                            break;
                        }

                        var existingVector = indiDevice.Properties.Get<IndiVector<IndiNumber>>(vector.Name);
                        var existingItem = existingVector.GetItemWithName(name) ?? throw new NullReferenceException();
                        var parsedValue = ParseIndiDouble(value, existingItem.Format);
                        
                        numberVector.Add(new IndiNumber(name, parsedValue));
                        break;
                    }
                    case IndiVector<IndiSwitch> switchVector:
                    {
                        switchVector.Add(new IndiSwitch(name, value == "On"));
                        break;
                    }
                    case IndiVector<IndiLight> lightVector:
                    {
                        lightVector.Add(new IndiLight(name, Enum.Parse<IndiState>(value)));
                        break;
                    }
                }
            }
        }
        
        // Assert this is the EndElement we expect
        if (reader.NodeType != XmlNodeType.EndElement ||
            !reader.Name.StartsWith("set") ||
            !reader.Name.EndsWith("Vector"))
            throw new InvalidOperationException("Malformed XML");
        
        var message = new IndiSetPropertyMessage(device, vector.Name, vector);
        return message;
    }

    private IndiDefPropertyMessage ParseDefVector(XmlReader reader, IndiVector vector, string device)
    {
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement &&
                reader.Name.StartsWith("def") &&
                reader.Name.EndsWith("Vector"))
                break;
            
            if (reader.NodeType == XmlNodeType.Element &&
                reader.Name == "defText" &&
                vector is IndiVector<IndiText> textVector)
            {
                var name = reader.GetAttribute("name") ?? string.Empty;
                var label = reader.GetAttribute("label") ?? string.Empty;

                // Get the text
                reader.Read();

                var text = reader.Value.Trim();

                // EndElement
                reader.Read();
                
                textVector.Add(new IndiText(name, label, text));
            }
            else if (reader.NodeType == XmlNodeType.Element &&
                     reader.Name == "defNumber" &&
                     vector is IndiVector<IndiNumber> numberVector)
            {
                var name = reader.GetAttribute("name") ?? string.Empty;
                var label = reader.GetAttribute("label") ?? string.Empty;
                var format = reader.GetAttribute("format") ?? string.Empty;
                var min = double.Parse(reader.GetAttribute("min") ?? "0");
                var max = double.Parse(reader.GetAttribute("max") ?? "0");
                var step = double.Parse(reader.GetAttribute("step") ?? "0");
                
                // Get the text
                reader.Read();

                var text = reader.Value.Trim();

                // EndElement
                reader.Read();

                numberVector.Add(new IndiNumber()
                {
                    Name = name,
                    Label = label,
                    Value = ParseIndiDouble(text, format),
                    Format = format,
                    Min = min,
                    Max = max,
                    Step = step,
                });
            }
            else if (reader.NodeType == XmlNodeType.Element &&
                     reader.Name == "defSwitch" &&
                     vector is IndiVector<IndiSwitch> switchVector)
            {
                var name = reader.GetAttribute("name") ?? string.Empty;
                var label = reader.GetAttribute("label") ?? string.Empty;

                // Get the text
                reader.Read();

                var text = reader.Value.Trim();

                // EndElement
                reader.Read();
                
                switchVector.Add(new IndiSwitch(name, label, text == "On"));
            }
            else if (reader.NodeType == XmlNodeType.Element &&
                reader.Name == "defLight" &&
                vector is IndiVector<IndiLight> lightVector)
            {
                var name = reader.GetAttribute("name") ?? string.Empty;
                var label = reader.GetAttribute("label") ?? string.Empty;
                
                // Get the text
                reader.Read();

                var text = reader.Value.Trim();

                // EndElement
                reader.Read();
                
                lightVector.Add(new IndiLight(name, label, Enum.Parse<IndiState>(text)));
            }
            else if (reader.NodeType == XmlNodeType.Element &&
                reader.Name == "defBLOB" &&
                vector is IndiVector<IndiBlob> blobVector)
            {
                var name = reader.GetAttribute("name") ?? string.Empty;
                var label = reader.GetAttribute("label") ?? string.Empty;
                blobVector.Add(new IndiBlob(name, label));
                
                // No EndElement for defBLOB
                continue;
            }
        }

        // Assert this is the EndElement we expect
        if (reader.NodeType != XmlNodeType.EndElement ||
            !reader.Name.StartsWith("def") ||
            !reader.Name.EndsWith("Vector"))
            throw new InvalidOperationException("Malformed XML");
        
        var message = new IndiDefPropertyMessage(device, vector.Name, vector);
        return message;
    }
    
    private static readonly Regex NumberFormat 
        = new(@"(?<d>[+-]?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)(?:\:(?<m>\d+(?:\.\d+)))?(?:\:(?<s>\d+(?:\.\d+)))?");
    
    private static double ParseIndiDouble(string value, string format)
    {
        /*
            From INDI spec:
            Can be printf formats 
                https://www.cplusplus.com/reference/cstdio/printf/
            Or custom formats
                %<w>.<f>m
                <w> is the total field width
                <f> is the width of the fraction. valid values are:
                    9 -> :mm:ss.ss
                    8 -> :mm:ss.s
                    6 -> :mm:ss
                    5 -> :mm.m
                    3 -> :mm
        */
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        format = format.ToLower();

        if (format.ToLower() == "x")
            return int.Parse(value, NumberStyles.HexNumber);

        var match = NumberFormat.Match(value);
        var degrees = double.Parse(match.Groups["d"].Value, NumberStyles.Float);
        if (match.Groups["m"].Success)
            degrees += double.Parse(match.Groups["d"].Value, NumberStyles.Float) / 60.0;
        if (match.Groups["s"].Success)
            degrees += double.Parse(match.Groups["s"].Value, NumberStyles.Float) / 3600.0;
        return degrees;
    }
    
    private void ProcessMessage(IIndiServerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        try
        {
            message.Process(this);
            
            OnMessageReceived(message);

            switch (message)
            {
                case IndiSetPropertyMessage setMessage:
                {
                    var device = Devices.GetDeviceOrThrow(setMessage.DeviceName);
                    OnSetPropertyReceived(device, setMessage.PropertyName, setMessage.PropertyValue);
                    break;
                }
                case IndiDefPropertyMessage defMessage:
                {
                    var device = Devices.GetDeviceOrThrow(defMessage.DeviceName);
                    OnDefinePropertyReceived(device, defMessage.PropertyName, defMessage.PropertyValue);
                    break;
                }
                case IndiDeletePropertyMessage delMessage:
                {
                    var device = Devices.GetDeviceOrThrow(delMessage.DeviceName);
                    if (device.Properties.TryGet(delMessage.PropertyName, out var vector))
                        OnDeletePropertyReceived(device, delMessage.PropertyName, vector!);
                    break;
                }
                case IndiNotificationMessage notificationMessage:
                {
                    var device = Devices.GetDeviceOrThrow(notificationMessage.DeviceName);
                    OnNotificationReceived(device, notificationMessage.Message);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception processing INDI server message {Xml}", message.ToXml());
        }
    }
}