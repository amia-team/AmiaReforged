using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration.JsonModels;

public sealed class QuantityJson
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}
