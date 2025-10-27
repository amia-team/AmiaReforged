using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// JSON converter for AreaTag value object.
/// </summary>
public class AreaTagJsonConverter : JsonConverter<AreaTag>
{
    public override AreaTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            return value != null ? new AreaTag(value) : throw new JsonException("AreaTag cannot be null");
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to AreaTag");
    }

    public override void Write(Utf8JsonWriter writer, AreaTag value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

