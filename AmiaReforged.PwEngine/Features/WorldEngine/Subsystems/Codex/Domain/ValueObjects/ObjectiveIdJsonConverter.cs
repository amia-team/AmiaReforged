using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Converts <see cref="ObjectiveId"/> to/from a plain JSON string so that
/// stages JSON produced by the Quest API (which uses string properties)
/// round-trips correctly into domain model <see cref="Entities.ObjectiveDefinition"/> objects.
/// </summary>
public sealed class ObjectiveIdJsonConverter : JsonConverter<ObjectiveId>
{
    public override ObjectiveId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? ObjectiveId.NewId() : new ObjectiveId(value);
    }

    public override void Write(Utf8JsonWriter writer, ObjectiveId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
