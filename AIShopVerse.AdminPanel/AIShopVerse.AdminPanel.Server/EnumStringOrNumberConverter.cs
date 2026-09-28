using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIShopVerse.AdminPanel.Server;

public sealed class EnumStringOrNumberConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(EnumStringOrNumberConverter<>).MakeGenericType(typeToConvert))!;
}

public sealed class EnumStringOrNumberConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (T)Enum.ToObject(typeToConvert, reader.GetInt64());
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (text != null && Enum.TryParse(typeToConvert, text, ignoreCase: true, out var parsed))
            {
                return (T)parsed;
            }
            throw new JsonException($"Value '{text}' is not valid for enum '{typeToConvert.Name}'.");
        }
        throw new JsonException($"Unexpected token {reader.TokenType} while reading enum '{typeToConvert.Name}'.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteNumberValue(Convert.ToInt64(value));
}