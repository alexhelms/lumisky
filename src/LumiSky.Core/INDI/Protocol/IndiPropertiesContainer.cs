using System.Collections;
using System.Collections.Concurrent;
using LumiSky.INDI.Primitives;

namespace LumiSky.INDI.Protocol;

public class IndiPropertiesContainer : IEnumerable<KeyValuePair<string, IndiVector>>
{
    private readonly ConcurrentDictionary<string, IndiVector> _properties = new();
    private readonly IndiDevice _owner;

    public IndiPropertiesContainer(IndiDevice owner)
    {
        _owner = owner;
    }

    public bool Exists(string name) => _properties.ContainsKey(name);

    /// <summary>
    /// Refresh all device properties.
    /// </summary>
    public async Task Refresh()
    {
        await _owner.Connection.Send(new IndiGetPropertiesMessage(_owner.Name));
    }
    
    /// <summary>
    /// Refresh a specific property.
    /// </summary>
    /// <param name="property">Name of the property to refresh.</param>
    public async Task Refresh(string property)
    {
        if (Exists(property) && Get(property) is { } vector)
            vector.State = IndiState.Busy;
        
        await _owner.Connection.Send(new IndiGetPropertiesMessage(_owner.Name, property));
    }

    public async Task Set(IndiVector vector, params IndiValue[] items)
    {
        // Mark the client vector as busy, aka "in-flight"
        vector.State = IndiState.Busy;

        // We don't send the client's vector, it has extra state information
        // that is not necessary for the device.
        var vectorToSend = vector.CreateNewVector(items);
        var message = new IndiNewPropertyMessage(_owner.Name, vector.Name, vectorToSend);
        _properties[vector.Name] = vector;
        await _owner.Connection.Send(message);
    }

    public IndiVector Get(string property)
    {
        return _properties[property];
    }

    public T Get<T>(string property)
        where T : IndiVector
    {
        return (T) _properties[property];
    }

    public bool TryGet(string property, out IndiVector? vector)
    {
        return _properties.TryGetValue(property, out vector);
    }

    public bool TryGet<T>(string property, out T value)
        where T : IndiVector, new()
    {
        value = new();
        
        if (Exists(property) && Get(property) is T t)
        {
            value = t;
            return true;
        }

        return false;
    }

    public void Clear() => _properties.Clear();

    public void Delete(string property) => _properties.Remove(property, out _);

    public IEnumerator<KeyValuePair<string, IndiVector>> GetEnumerator() => _properties.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _properties.GetEnumerator();

    public IndiVector this[string key]
    {
        get => Get(key);
        internal set => _properties[key] = value;
    }
}