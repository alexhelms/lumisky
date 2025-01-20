using System.Text.Json;
using System.Text.Json.Serialization;

namespace LumiSky.Core.Indi;

public class CustomIndiPropertyTypeConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.True => true,
            JsonTokenType.False => true,
            JsonTokenType.String => reader.GetString(),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
