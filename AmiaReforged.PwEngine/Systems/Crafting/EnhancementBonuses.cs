using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

public class EnhancementBonuses
{
    public CraftingProperty EnhancementBonusOne => new()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyEnhancementBonus(1)!,
        GuiLabel = "+1",
        Category = PropertyCategory.EnhancementBonus,
        SupportedItemTypes = ItemTypeConstants.MeleeWeapons()
    };

    public static CraftingProperty EnhancementBonusTwo => new()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyEnhancementBonus(2)!,
        GuiLabel = "+2",
        Category = PropertyCategory.EnhancementBonus,
        SupportedItemTypes = ItemTypeConstants.MeleeWeapons()
    };

    public static CraftingProperty EnhancementBonusThree => new()
    {
        Cost = 2,
        Property = NWScript.ItemPropertyEnhancementBonus(3)!,
        GuiLabel = "+3",
        Category = PropertyCategory.EnhancementBonus,
        SupportedItemTypes = ItemTypeConstants.MeleeWeapons()
    };

    public static CraftingProperty EnhancementBonusFour => new()
    {
        Cost = 2,
        Property = NWScript.ItemPropertyEnhancementBonus(4)!,
        GuiLabel = "+4",
        Category = PropertyCategory.EnhancementBonus,
        SupportedItemTypes = ItemTypeConstants.MeleeWeapons()
    };

    public static CraftingProperty EnhancementBonusFive => new()
    {
        Cost = 4,
        Property = NWScript.ItemPropertyEnhancementBonus(5)!,
        GuiLabel = "+5",
        Category = PropertyCategory.EnhancementBonus,
        SupportedItemTypes = ItemTypeConstants.MeleeWeapons()
    };
}