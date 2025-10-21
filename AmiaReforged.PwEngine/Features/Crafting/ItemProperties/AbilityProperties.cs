using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

public static class AbilityProperties
{
    private const int MythalCostAbility1 = 2000;
    private const int MythalCostAbility2 = 10000;
    private const int MythalCostAbility3 = 30000;

    public static readonly CraftingCategory Abilities = new(categoryId: "ability_selection")
    {
        Label = "Ability Bonus",
        Properties =
        [
            // Intermediate
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 1)!,
                GuiLabel = "+1 Strength",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 1)!,
                GuiLabel = "+1 Dexterity",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 1)!,
                GuiLabel = "+1 Constitution",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 1)!,
                GuiLabel = "+1 Intelligence",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 1)!,
                GuiLabel = "+1 Wisdom",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 1)!,
                GuiLabel = "+1 Charisma",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate,
                Tags = ["Ability"]
            },
            // Greater
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 2)!,
                GuiLabel = "+2 Strength",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 2)!,
                GuiLabel = "+2 Dexterity",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 2)!,
                GuiLabel = "+2 Constitution",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 2)!,
                GuiLabel = "+2 Intelligence",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 2)!,
                GuiLabel = "+2 Wisdom",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 2)!,
                GuiLabel = "+2 Charisma",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater,
                Tags = ["Ability"]
            },
            // Flawless
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 3)!,
                GuiLabel = "+3 Strength",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 3)!,
                GuiLabel = "+3 Dexterity",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 3)!,
                GuiLabel = "+3 Constitution",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 3)!,
                GuiLabel = "+3 Intelligence",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 3)!,
                GuiLabel = "+3 Wisdom",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 3)!,
                GuiLabel = "+3 Charisma",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless,
                Tags = ["Ability"]
            },
            // Dreamcoin - +4
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 4)!,
                GuiLabel = "+4 Strength",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 4)!,
                GuiLabel = "+4 Dexterity",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 4)!,
                GuiLabel = "+4 Constitution",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 4)!,
                GuiLabel = "+4 Intelligence",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 4)!,
                GuiLabel = "+4 Wisdom",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 4)!,
                GuiLabel = "+4 Charisma",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            // Dreamcoin - +5
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 5)!,
                GuiLabel = "+5 Strength",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 5)!,
                GuiLabel = "+5 Dexterity",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 5)!,
                GuiLabel = "+5 Constitution",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 5)!,
                GuiLabel = "+5 Intelligence",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 5)!,
                GuiLabel = "+5 Wisdom",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 5)!,
                GuiLabel = "+5 Charisma",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            // DreamCoin - +6
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 6)!,
                GuiLabel = "+6 Strength",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 6)!,
                GuiLabel = "+6 Dexterity",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 6)!,
                GuiLabel = "+6 Constitution",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 6)!,
                GuiLabel = "+6 Intelligence",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 6)!,
                GuiLabel = "+6 Wisdom",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            },
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 6)!,
                GuiLabel = "+6 Charisma",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Wondrous,
                Tags = ["Ability"]
            }
        ],
        BaseDifficulty = 5
    };
}
