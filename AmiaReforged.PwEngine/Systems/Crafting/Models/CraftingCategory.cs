using AmiaReforged.PwEngine.Systems.NwObjectHelpers;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingCategory
{
    public string CategoryId { get; set; }

    public CraftingCategory(string categoryId)
    {
        CategoryId = categoryId;
    }

    public int BaseDifficulty { get; init; }
    public required string Label { get; init; }
    public required IReadOnlyList<CraftingProperty> Properties { get; init; }
}