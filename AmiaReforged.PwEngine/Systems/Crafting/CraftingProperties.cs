using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

/// <summary>
/// I felt it was better to use fixed properties for crafting, rather than a dynamic system, because it can get
/// difficult to manage and balance.
/// </summary>
public static class CraftingProperties
{
    public static CraftingProperty EnhancementBonusOne => new()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyEnhancementBonus(1)!,
        Category = PropertyCategory.EnhancementBonus,
        SupportedItemTypes = ItemTypeConstants.MeleeWeapons()
    };
    
    public static CraftingProperty AcBonusOne => new()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyACBonus(1)!,
        Category = PropertyCategory.ArmorClass,
        SupportedItemTypes = ItemTypeConstants.EquippableItems()
    };
}