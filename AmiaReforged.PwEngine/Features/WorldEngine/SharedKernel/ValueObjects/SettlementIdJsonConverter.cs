using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// JSON converter for SettlementId value object.
/// </summary>
public class SettlementIdJsonConverter : JsonConverter<SettlementId>
{
    public override SettlementId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            int value = reader.GetInt32();
            try
            {
                return SettlementId.Parse(value);
            }
            catch (ArgumentException ex)
            {
                throw new JsonException($"Invalid settlement ID {value}: Settlement IDs must be positive integers", ex);
            }
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to SettlementId");
    }

    public override void Write(Utf8JsonWriter writer, SettlementId value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

