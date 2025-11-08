using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spamma.Modules.UserManagement.Infrastructure.JsonConverters;

public class ByteArrayJsonConverter : JsonConverter<byte[]>
{
    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var base64String = reader.GetString();
            return string.IsNullOrEmpty(base64String) ? [] : Convert.FromBase64String(base64String);
        }

        throw new JsonException("Expected string token for byte array");
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(Convert.ToBase64String(value));
        }
    }
}