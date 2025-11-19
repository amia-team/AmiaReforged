using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

public sealed record JsonLocalVariableDefinition(
    string Name,
    JsonLocalVariableType Type,
    JsonElement Value);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JsonLocalVariableType
{
    Int,
    String,
    Json
}

