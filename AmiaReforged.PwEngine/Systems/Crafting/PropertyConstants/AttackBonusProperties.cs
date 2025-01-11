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
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(1)!,
                GuiLabel = "+1",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(4)!,
                GuiLabel = "+4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAttackBonus(5)!,
                GuiLabel = "+5",
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
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(1)!,
                GuiLabel = "+1",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(4)!,
                GuiLabel = "+4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(5)!,
                GuiLabel = "+5",
                CraftingTier = CraftingTier.Flawless
            }
        }
    };
}