using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration.JsonModels;

public sealed class ReactionJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("inputs")]
    public List<QuantityJson> Inputs { get; set; } = [];

    [JsonPropertyName("outputs")]
    public List<QuantityJson> Outputs { get; set; } = [];

    [JsonPropertyName("baseDuration")]
    public string BaseDuration { get; set; } = string.Empty;

    [JsonPropertyName("baseSuccessChance")]
    public double BaseSuccessChance { get; set; }

    [JsonPropertyName("preconditions")]
    public List<PreconditionJson>? Preconditions { get; set; }

    [JsonPropertyName("modifiers")]
    public List<ModifierJson>? Modifiers { get; set; }
}
