using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting;

/// <summary>
///  Plain old data object for crafting properties. Should not contain any logic (i.e.: Can the property stack).
/// </summary>
public class CraftingProperty
{
    public required ItemProperty Property { get; init; }
    // public PropertyCategory Category { get; init; }
    public required string GuiLabel { get; init; }
    public required int Cost { get; init; }
    
    public required CraftingTier CraftingTier { get; set; }
    
}