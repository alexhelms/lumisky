using LumiSky.Core.Indi.Serialization;
using Nito.AsyncEx;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Xml.Linq;

namespace LumiSky.Core.Indi;

public class IndiClient : IDisposable
{
    private IndiConnection? _connection;
    private CancellationTokenSource? _connectionCts;
    private ConcurrentDictionary<string, IndiDevice> _devices = [];
    private AsyncMonitor _newDeviceSignal = new();

    public bool IsConnected => _connection?.IsConnected ?? false;

    public bool LogIndiMessages { get; set; }

    public event EventHandler<string>? DeviceAdded;
    public event EventHandler<string>? DeviceRemoved;

    private void ThrowIfNotConnected()
    {
        if (_connection is null || !_connection.IsConnected)
        {
            throw new NotConnectedException();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _connection?.Dispose();
    }

    public async Task Connect(
        string hostname, 
        int port = 7624, 
        CancellationToken token = default)
    {
        if (_connection?.IsConnected ?? false)
            throw new AlreadyConnectedException();

        _connection = new IndiConnection();
        var reader = await _connection.Connect(hostname, port, token);

        // Setup the process thread
        _connectionCts = new CancellationTokenSource();
        _ = Task.Factory.StartNew(ProcessThread, reader, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

        await _connection.RefreshProperties();
    }

    public void Disconnect()
    {
        _connectionCts?.Cancel();
        _connection?.Disconnect();
        _connection = null;
    }

    public async Task<IndiDevice> GetDevice(string name, CancellationToken token = default)
    {
        ThrowIfNotConnected();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(300));

        try
        {
            using (await _newDeviceSignal.EnterAsync(cts.Token))
            {
                while (true)
                {
                    if (_devices.TryGetValue(name, out IndiDevice? device))
                    {
                        return device;
                    }

                    await _newDeviceSignal.WaitAsync(cts.Token);
                }
            }
        }
        catch (TaskCanceledException)
        {
            if (cts.IsCancellationRequested && !token.IsCancellationRequested)
                throw new TimeoutException($"{name} device not found.");

            throw;
        }
    }

    private async Task ProcessThread(object? state)
    {
        if (state is not ChannelReader<IIndiCommand> reader)
            throw new ArgumentNullException(nameof(state));

        if (_connectionCts is null)
            throw new NullReferenceException("Connection cts should not be null");

        try
        {
            await foreach (var command in reader.ReadAllAsync(_connectionCts.Token))
            {
                //Log.Information(command.ToString() ?? string.Empty);

                // Dispatch the command
                Func<Task> func = command switch
                {
                    DefVector defVector => () => AddNewParameter(defVector),
                    SetVector setVector => () => UpdateParameter(setVector),
                    DelProperty delProperty => () => DeleteParameter(delProperty),
                    Message message => () => LogMessage(message),
                    _ => static () => Task.CompletedTask,
                };

                try
                {
                    await func();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error processing INDI command {Name}", command.GetType().Name);
                }
            }
        }
        finally
        {
            _connectionCts?.Dispose();
            _connectionCts = null;
        }
    }

    private async Task<IndiDevice> GetOrCreateDevice(string name)
    {
        if (_connection is null) throw new NullReferenceException();

        IndiDevice? device;
        if (!_devices.TryGetValue(name, out device))
        {
            // New device found
            device = new IndiDevice(_connection)
            {
                Name = name,
            };

            _devices[name] = device;

            // Notify anybody waiting for a device to be added.
            using (await _newDeviceSignal.EnterAsync())
            {
                _newDeviceSignal.PulseAll();
            }

            DeviceAdded?.Invoke(this, name);
        }

        return device;
    }

    private async Task AddNewParameter(DefVector command)
    {
        var device = await GetOrCreateDevice(command.Device);
        await device.AddParameter(command);
    }

    private async Task UpdateParameter(SetVector command)
    {
        var device = await GetOrCreateDevice(command.Device);
        device.UpdateParameter(command);
    }

    private async Task DeleteParameter(DelProperty command)
    {
        if (!string.IsNullOrWhiteSpace(command.Device))
        {
            if (_devices.ContainsKey(command.Device))
            {
                // Stop tracking all properties of the given device
                while (!_devices.TryRemove(command.Device, out _)) ;

                DeviceRemoved?.Invoke(this, command.Device);
            }
        }
        else
        {
            var device = await GetOrCreateDevice(command.Device);

            // Stop tracking a single property
            device.DeleteParameter(command);
        }
    }

    private Task LogMessage(Message message)
    {
        if (LogIndiMessages)
        {
            Log.Information(message.ToString());
        }

        return Task.CompletedTask;
    }
}
