namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingPropertyCategory
{
    public required string Label { get; set; }
    public required IReadOnlyList<CraftingProperty> Properties { get; set; }
}