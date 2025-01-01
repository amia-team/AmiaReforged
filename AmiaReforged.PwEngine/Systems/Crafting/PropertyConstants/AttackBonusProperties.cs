using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class AttackBonusProperties
{
    public static readonly CraftingCategory AttackBonus = new("attack_bonus")
    {
        Label = "Attack Bonus",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(1)!,
                GuiLabel = "+1 Attack Bonus",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(2)!,
                GuiLabel = "+2 Attack Bonus",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(3)!,
                GuiLabel = "+3 Attack Bonus",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(4)!,
                GuiLabel = "+4 Attack Bonus",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyAttackBonus(5)!,
                GuiLabel = "+5 Attack Bonus",
                CraftingTier = CraftingTier.Flawless
            },
        }
    };

    public static readonly CraftingCategory EnhancementBonus = new("enhancement_bonus")
    {
        Label = "Enhancement Bonus",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(1)!,
                GuiLabel = "+1 Enhancement Bonus",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(2)!,
                GuiLabel = "+2 Enhancement Bonus",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(3)!,
                GuiLabel = "+3 Enhancement Bonus",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(4)!,
                GuiLabel = "+4 Enhancement Bonus",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(5)!,
                GuiLabel = "+5 Enhancement Bonus",
                CraftingTier = CraftingTier.Flawless
            }
        }
    };
}