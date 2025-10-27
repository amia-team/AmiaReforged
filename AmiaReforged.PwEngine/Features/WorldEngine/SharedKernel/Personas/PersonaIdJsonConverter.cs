using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// JSON converter for PersonaId value object.
/// Serializes as string in "Type:Value" format.
/// </summary>
public class PersonaIdJsonConverter : JsonConverter<PersonaId>
{
    public override PersonaId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value == null)
                throw new JsonException("PersonaId cannot be null");

            try
            {
                return PersonaId.Parse(value);
            }
            catch (ArgumentException ex)
            {
                throw new JsonException($"Invalid PersonaId: {ex.Message}", ex);
            }
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to PersonaId");
    }

    public override void Write(Utf8JsonWriter writer, PersonaId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

