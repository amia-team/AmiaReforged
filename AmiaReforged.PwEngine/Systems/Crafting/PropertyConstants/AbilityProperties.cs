using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class AbilityProperties
{
    public static readonly IReadOnlyList<CraftingProperty> Abilities = new[]
    {
        // Intermediate
        new CraftingProperty()
        {
            Cost = 2,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 1)!,
            GuiLabel = "+1 Strength",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty()
        {
            Cost = 2,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 1)!,
            GuiLabel = "+1 Dexterity",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty()
        {
            Cost = 2,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 1)!,
            GuiLabel = "+1 Constitution",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty()
        {
            Cost = 2,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 1)!,
            GuiLabel = "+1 Intelligence",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty()
        {
            Cost = 2,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 1)!,
            GuiLabel = "+1 Wisdom",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty()
        {
            Cost = 2,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 1)!,
            GuiLabel = "+1 Charisma",
            CraftingTier = CraftingTier.Intermediate
        },
        // Greater
        new CraftingProperty()
        {
            Cost = 4,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 2)!,
            GuiLabel = "+2 Strength",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty()
        {
            Cost = 4,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 2)!,
            GuiLabel = "+2 Dexterity",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty()
        {
            Cost = 4,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 2)!,
            GuiLabel = "+2 Constitution",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty()
        {
            Cost = 4,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 2)!,
            GuiLabel = "+2 Intelligence",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty()
        {
            Cost = 4,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 2)!,
            GuiLabel = "+2 Wisdom",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty()
        {
            Cost = 4,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 2)!,
            GuiLabel = "+2 Charisma",
            CraftingTier = CraftingTier.Greater
        },
        // Flawless
        new CraftingProperty()
        {
            Cost = 6,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 3)!,
            GuiLabel = "+3 Strength",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty()
        {
            Cost = 6,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 3)!,
            GuiLabel = "+3 Dexterity",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty()
        {
            Cost = 6,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 3)!,
            GuiLabel = "+3 Constitution",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty()
        {
            Cost = 6,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 3)!,
            GuiLabel = "+3 Intelligence",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty()
        {
            Cost = 6,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 3)!,
            GuiLabel = "+3 Wisdom",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty()
        {
            Cost = 6,
            Property = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 3)!,
            GuiLabel = "+3 Charisma",
            CraftingTier = CraftingTier.Flawless
        }
    };
}