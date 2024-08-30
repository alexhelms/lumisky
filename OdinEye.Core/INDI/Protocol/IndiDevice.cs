using System.Diagnostics;
using System.Text;
using OdinEye.INDI.Primitives;

namespace OdinEye.INDI.Protocol;

public class IndiDevice
{
    public string Name { get; }
    public IndiConnection Connection { get; }
    public IndiPropertiesContainer Properties { get; }

    public IndiDevice(string name, IndiConnection connection)
    {
        Name = name;
        Connection = connection;
        Properties = new(this);
    }

    public async Task Set<T>(string propertyName, string elementName, object value, TimeSpan timeout = default, CancellationToken token = default)
        where T : IndiValue
    {
        var vector = Properties.Get<IndiVector<T>>(propertyName);
        if (vector.GetItemWithName(elementName) is { } item)
        {
            var timeoutCts = new CancellationTokenSource(timeout == default ? TimeSpan.FromHours(1) : timeout);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);

            item.TryUpdateValue(value);
            await Properties.Set(vector, item);

            try
            {
                while (vector.IsBusy && !linkedCts.Token.IsCancellationRequested)
                    await Task.Delay(10, linkedCts.Token);
            }   
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
        }
        else
        {
            throw new KeyNotFoundException($"{propertyName}:{elementName} not found");
        }
    }

    public async Task Set<T>(string propertyName, string[] elementNames, object[] values, TimeSpan timeout = default, CancellationToken token = default)
        where T : IndiValue
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(elementNames.Length, values.Length);

        var vector = Properties.Get<IndiVector<T>>(propertyName);
        var items = new List<IndiValue>();
        foreach (var (elementName, value) in elementNames.Zip(values))
        {
            if (vector.GetItemWithName(elementName) is { } item)
            {
                item.TryUpdateValue(value);
                items.Add(item);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName}:{elementName} not found");
            }
        }

        var timeoutCts = new CancellationTokenSource(timeout == default ? TimeSpan.FromHours(1) : timeout);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);

        await Properties.Set(vector, items.ToArray());

        try
        {
            while (vector.IsBusy && !linkedCts.Token.IsCancellationRequested)
                await Task.Delay(10, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException();
            }
        }
    }

    public T Get<T>(string propertyName, string elementName)
        where T : IndiValue
    {
        if (TryGet<T>(propertyName, elementName, out var value))
        {
            return value!;
        }

        throw new KeyNotFoundException($"{propertyName}:{elementName} not found");
    }

    public bool TryGet<T>(string propertyName, string elementName, out T? result)
        where T : IndiValue
    {
        result = default;

        if (Properties.TryGet<IndiVector<T>>(propertyName, out var vec)
            && vec.TryGetItemWithName(elementName, out var element))
        {
            result = element;
            return true;
        }

        return false;
    }

    public IndiVector Get(string propertyName) => Properties.Get(propertyName);

    public bool Has(string propertyName) => Properties.Exists(propertyName);

    public async Task Connect()
    {
        await Set<IndiSwitch>("CONNECTION", "CONNECT", true);

        var sw = Stopwatch.StartNew();
        Connection.DefinePropertyReceived += ConnectionOnDefinePropertyReceived;

        try
        {
            // getProperties is sent and indiserver responds with def messages.
            // Wait until the line goes silent for a brief time so we know we got everything.
            await Properties.Refresh();
            await Connection.Send(new IndiEnableBlobMessage(Name, IndiBlobState.Also));
            
            while (sw.Elapsed < TimeSpan.FromMilliseconds(250))
            {
                await Task.Delay(25);
            }
        }
        finally
        {
            sw.Stop();
            Connection.DefinePropertyReceived -= ConnectionOnDefinePropertyReceived;
        }
        
        void ConnectionOnDefinePropertyReceived(IndiDevice device, string property, IndiVector value)
        {
            sw.Restart();
        }
    }

    public async Task Disconnect()
    {
        await Set<IndiSwitch>("CONNECTION", "DISCONNECT", true);
        await Properties.Refresh();
    }

    public string DumpProperties()
    {
        var sb = new StringBuilder(1024);

        foreach (var (name, vector) in Properties)
        {
            sb.AppendLine($"{name}");

            if (vector is IndiVector<IndiSwitch> indiSwitch)
            {
                foreach (var item in indiSwitch)
                    sb.AppendLine($"    {item.Name}: {item.Value}");
            }
            else if (vector is IndiVector<IndiNumber> indiNumber)
            {
                foreach (var item in indiNumber)
                    sb.AppendLine($"    {item.Name}: {item.Value}");
            }
            else if (vector is IndiVector<IndiText> indiText)
            {
                foreach (var item in indiText)
                    sb.AppendLine($"    {item.Name}: {item.Value}");
            }
            else if (vector is IndiVector<IndiBlob> indiBlob)
            {
                foreach (var item in indiBlob)
                    sb.AppendLine($"    {item.Name}: {item.Value}");
            }
            else
            {
                sb.AppendLine(vector.ToString());
            }
        }

        return sb.ToString();
    }

    public bool IsConnected => Get<IndiSwitch>("CONNECTION", "CONNECT")?.Value ?? false;
}