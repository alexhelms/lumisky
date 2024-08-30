using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OdinEye.Core.Serialization.Converters;

public class InterfaceConverter<TInterface, TImpl> : JsonConverter
{
    public override bool CanWrite => false;
    public override bool CanRead => true;
    
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TInterface);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new InvalidOperationException("Use default serialization.");
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var deserialized = Activator.CreateInstance<TImpl>() ?? throw new NullReferenceException();
        serializer.Populate(jsonObject.CreateReader(), deserialized);
        return deserialized;
    }
}
