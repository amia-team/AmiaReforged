using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration.JsonModels;

public sealed class ModifierJson
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}
