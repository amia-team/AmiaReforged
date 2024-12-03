using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting;

public class CraftingProperty
{
    public required ItemProperty Property { get; init; }
    public PropertyCategory Category { get; init; }
    public string GuiLabel { get; init; }
    public required int Cost { get; init; }
    public required List<int> SupportedItemTypes { get; init; }    
}