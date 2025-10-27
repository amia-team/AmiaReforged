using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// JSON converter for RegionTag value object.
/// </summary>
public class RegionTagJsonConverter : JsonConverter<RegionTag>
{
    public override RegionTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            return value != null ? new RegionTag(value) : throw new JsonException("RegionTag cannot be null");
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to RegionTag");
    }

    public override void Write(Utf8JsonWriter writer, RegionTag value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

