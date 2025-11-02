using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed record NpcShopDefinition(
    string Tag,
    string DisplayName,
    string ShopkeeperTag,
    string? Description,
    NpcShopRestockDefinition Restock,
    IReadOnlyList<NpcShopProductDefinition> Products);

public sealed record NpcShopProductDefinition(
    string ResRef,
    int Price,
    int InitialStock,
    int MaxStock,
    int RestockAmount);

public sealed record NpcShopRestockDefinition(
    [property: JsonPropertyName("MinMinutes")] int MinMinutes,
    [property: JsonPropertyName("MaxMinutes")] int MaxMinutes);
