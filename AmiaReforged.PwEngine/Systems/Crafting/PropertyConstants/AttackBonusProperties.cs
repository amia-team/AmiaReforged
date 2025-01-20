using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class AttackBonusProperties
{
    private const int MythalCostAb1 = 500;
    private const int MythalCostAb2 = 2000;
    private const int MythalCostAb3 = 5000;
    private const int MythalCostAb4 = 15000;
    private const int MythalCostAb5 = 35000;

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
                GoldCost = MythalCostAb1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(2)!,
                GuiLabel = "+2",
                GoldCost = MythalCostAb2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(3)!,
                GuiLabel = "+3",
                GoldCost = MythalCostAb3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(4)!,
                GuiLabel = "+4",
                GoldCost = MythalCostAb4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAttackBonus(5)!,
                GuiLabel = "+5",
                GoldCost = MythalCostAb5,
                CraftingTier = CraftingTier.Flawless
            }
        },

        PerformValidation = (_, i) =>
        {
            return i.ItemProperties.Any(ip => ip.Property.PropertyType == ItemPropertyType.AttackBonus)
                ? PropertyValidationResult.BasePropertyMustBeUnique
                : PropertyValidationResult.Valid;
        }
    };

    private const int MythalCostEnhancement1 = 2000;
    private const int MythalCostEnhancement2 = 5000;
    private const int MythalCostEnhancement3 = 15000;
    private const int MythalCostEnhancement4 = 35000;
    private const int MythalCostEnhancement5 = 75000;

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
                GoldCost = MythalCostEnhancement1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(2)!,
                GuiLabel = "+2",
                GoldCost = MythalCostEnhancement2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(3)!,
                GuiLabel = "+3",
                GoldCost = MythalCostEnhancement3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(4)!,
                GuiLabel = "+4",
                GoldCost = MythalCostEnhancement4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(5)!,
                GuiLabel = "+5",
                GoldCost = MythalCostEnhancement5,
                CraftingTier = CraftingTier.Flawless
            }
        },
        PerformValidation = (_, i) =>
        {
            return i.ItemProperties.Any(ip => ip.Property.PropertyType == ItemPropertyType.EnhancementBonus)
                ? PropertyValidationResult.BasePropertyMustBeUnique
                : PropertyValidationResult.Valid;
        }
    };
}