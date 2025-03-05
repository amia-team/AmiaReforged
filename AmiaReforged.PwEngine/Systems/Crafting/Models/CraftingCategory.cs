using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingCategory
{
    public CraftingCategory(string categoryId)
    {
        CategoryId = categoryId;
    }

    public string CategoryId { get; set; }

    public int BaseDifficulty { get; init; }
    public required string Label { get; init; }
    public required IReadOnlyList<CraftingProperty> Properties { get; init; }

    public bool ExclusiveToClass { get; init; }
    public ClassType? ExclusiveClass { get; init; }
}