using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

public class ArmorClassBonuses
{
    public CraftingProperty AcBonusOne => new()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyACBonus(1)!,
        GuiLabel = "+1",
        Category = PropertyCategory.ArmorClass,
        SupportedItemTypes = ItemTypeConstants.EquippableItems()
    };

    public static CraftingProperty AcBonusTwo => new()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyACBonus(2)!,
        GuiLabel = "+2",
        Category = PropertyCategory.ArmorClass,
        SupportedItemTypes = ItemTypeConstants.EquippableItems()
    };

    public static CraftingProperty AcBonusThree => new()
    {
        Cost = 2,
        Property = NWScript.ItemPropertyACBonus(3)!,
        GuiLabel = "+3",
        Category = PropertyCategory.ArmorClass,
        SupportedItemTypes = ItemTypeConstants.EquippableItems()
    };

    public static CraftingProperty AcBonusFour => new()
    {
        Cost = 2,
        Property = NWScript.ItemPropertyACBonus(4)!,
        GuiLabel = "+4",
        Category = PropertyCategory.ArmorClass,
        SupportedItemTypes = ItemTypeConstants.EquippableItems()
    };

    public static CraftingProperty AcBonusFive => new()
    {
        Cost = 4,
        Property = NWScript.ItemPropertyACBonus(5)!,
        GuiLabel = "+5",
        Category = PropertyCategory.ArmorClass,
        SupportedItemTypes = ItemTypeConstants.EquippableItems()
    };
}