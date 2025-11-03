using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed record NpcShopDefinition(
    string Tag,
    string DisplayName,
    string ShopkeeperTag,
    string? Description,
    NpcShopRestockDefinition Restock,
    IReadOnlyList<NpcShopProductDefinition> Products,
    IReadOnlyList<int>? AcceptCategories = null,
    int MarkupPercent = 0);

public sealed record NpcShopProductDefinition(
    string ResRef,
    string Name,
    string? Description,
    int Price,
    int InitialStock,
    int MaxStock,
    int RestockAmount,
    int? BaseItemType = null,
    IReadOnlyList<JsonLocalVariableDefinition>? LocalVariables = null,
    SimpleModelAppearanceDefinition? Appearance = null);

public sealed record NpcShopRestockDefinition(
    [property: JsonPropertyName("MinMinutes")] int MinMinutes,
    [property: JsonPropertyName("MaxMinutes")] int MaxMinutes);

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

public sealed record SimpleModelAppearanceDefinition(int ModelType, int? SimpleModelNumber);
