using LumiSky.Core.Indi.Parameters;
using LumiSky.Core.Indi.Serialization;
using Nito.AsyncEx;
using System.Collections.Concurrent;

namespace LumiSky.Core.Indi;

public class IndiDevice
{
    private AsyncMonitor _newParameterSignal = new();

    private readonly IndiConnection _connection;
    private readonly ConcurrentDictionary<string, IndiParameter> _parameters = [];

    public required string Name { get; init; }

    public IndiDevice(IndiConnection connection)
    {
        _connection = connection;
    }

    private void ThrowIfNotConnected()
    {
        if (!_connection.IsConnected)
        {
            throw new NotConnectedException();
        }
    }

    public bool HasParameter(string name)
    {
        return _parameters.ContainsKey(name);
    }

    public async Task AddParameter(DefVector command)
    {
        if (!_parameters.ContainsKey(command.Name))
        {
            // Create a new parameter so it can be tracked
            var parameter = new IndiParameter(command);
            _parameters[parameter.Name] = parameter;

            // Notify anybody waiting for a parameter to be added.
            using (await _newParameterSignal.EnterAsync())
            {
                _newParameterSignal.PulseAll();
            }
        }
    }

    public void UpdateParameter(SetVector command)
    {
        if (_parameters.TryGetValue(command.Name, out var parameter))
        {
            parameter.Update(command);
        }
    }

    public void DeleteParameter(DelProperty command)
    {
        if (command.Name is not null &&
            _parameters.ContainsKey(command.Name))
        {
            while (!_parameters.TryRemove(command.Name, out _)) ;
        }
    }

    public async Task EnableBlobs()
    {
        ThrowIfNotConnected();
        await _connection.EnableBlobs(Name);
    }

    public async Task<IndiParameter> GetParameter(string name, TimeSpan timeout = default)
    {
        ThrowIfNotConnected();

        if (timeout == default)
            timeout = TimeSpan.FromSeconds(5);

        using var cts = new CancellationTokenSource(timeout);

        try
        {
            // Is it already there?
            if (_parameters.TryGetValue(name, out IndiParameter? parameter))
            {
                return parameter;
            }

            using (await _newParameterSignal.EnterAsync(cts.Token))
            {
                while (true)
                {
                    if (_parameters.TryGetValue(name, out parameter))
                    {
                        return parameter;
                    }

                    await _newParameterSignal.WaitAsync(cts.Token);
                }
            }
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"{name} parameter not found.");
        }
    }

    public async Task Change(
        string parameterName,
        IEnumerable<(string, object)> values,
        TimeSpan timeout = default,
        CancellationToken token = default)
    {
        ThrowIfNotConnected();

        var parameter = await GetParameter(parameterName, timeout);

        if (timeout == default)
            timeout = TimeSpan.FromSeconds(5);

        var tcs = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        void Handler(object? sender, EventArgs e)
        {
            if (parameter.AreItemsEquivalent(values))
            {
                tcs.SetResult();
                parameter.Updated -= Handler;
            }
        }

        try
        {
            using (cts.Token.Register(OnTimeout))
            {
                parameter.Updated += Handler;
                var command = parameter.ToCommand(values);
                parameter.SetBusy();
                await _connection.SendCommand(command);
                await tcs.Task;
            }
        }
        finally
        {
            parameter.Updated -= Handler;
        }

        void OnTimeout()
        {
            if (cts.IsCancellationRequested && !token.IsCancellationRequested)
            {
                tcs.SetException(new TimeoutException($"Timed out changing {parameterName}"));
            }
            else
            {
                tcs.SetCanceled(cts.Token);
            }
        }
    }

    public async Task WaitForChange(
        string parameterName,
        TimeSpan timeout = default,
        CancellationToken token = default)
    {
        ThrowIfNotConnected();

        var parameter = await GetParameter(parameterName, timeout);

        if (timeout == default)
            timeout = TimeSpan.FromSeconds(5);

        var tcs = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        void Handler(object? sender, EventArgs e)
        {
            tcs.SetResult();
            parameter.Updated -= Handler;
        }

        try
        {
            using (cts.Token.Register(OnTimeout))
            {
                parameter.Updated += Handler;
                await tcs.Task;
            }
        }
        finally
        {
            parameter.Updated -= Handler;
        }

        void OnTimeout()
        {
            if (cts.IsCancellationRequested && !token.IsCancellationRequested)
            {
                tcs.SetException(new TimeoutException($"Timed out waiting for {parameterName} to change"));
            }
            else
            {
                tcs.SetCanceled(cts.Token);
            }
        }
    }
}
