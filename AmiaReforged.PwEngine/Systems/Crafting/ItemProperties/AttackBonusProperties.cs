using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public static class AttackBonusProperties
{
    private const int MythalCostAb1 = 500;
    private const int MythalCostAb2 = 2000;
    private const int MythalCostAb3 = 5000;
    private const int MythalCostAb4 = 15000;
    private const int MythalCostAb5 = 35000;

    private const int MythalCostEnhancement1 = 2000;
    private const int MythalCostEnhancement2 = 5000;
    private const int MythalCostEnhancement3 = 15000;
    private const int MythalCostEnhancement4 = 35000;
    private const int MythalCostEnhancement5 = 75000;

    public static readonly CraftingCategory AttackBonus = new(categoryId: "attack_bonus")
    {
        Label = "Attack Bonus",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(1)!,
                GuiLabel = "+1 Attack Bonus",
                GoldCost = MythalCostAb1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(2)!,
                GuiLabel = "+2 Attack Bonus",
                GoldCost = MythalCostAb2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(3)!,
                GuiLabel = "+3 Attack Bonus",
                GoldCost = MythalCostAb3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyAttackBonus(4)!,
                GuiLabel = "+4 Attack Bonus",
                GoldCost = MythalCostAb4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAttackBonus(5)!,
                GuiLabel = "+5 Attack Bonus",
                GoldCost = MythalCostAb5,
                CraftingTier = CraftingTier.Flawless
            }
        ],
        BaseDifficulty = 5
    };

    public static readonly CraftingCategory EnhancementBonus = new(categoryId: "enhancement_bonus")
    {
        Label = "Enhancement Bonus",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(1)!,
                GuiLabel = "+1 Enhancement Bonus",
                GoldCost = MythalCostEnhancement1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(2)!,
                GuiLabel = "+2 Enhancement Bonus",
                GoldCost = MythalCostEnhancement2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(3)!,
                GuiLabel = "+3 Enhancement Bonus",
                GoldCost = MythalCostEnhancement3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(4)!,
                GuiLabel = "+4 Enhancement Bonus",
                GoldCost = MythalCostEnhancement4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyEnhancementBonus(5)!,
                GuiLabel = "+5 Enhancement Bonus",
                GoldCost = MythalCostEnhancement5,
                CraftingTier = CraftingTier.Flawless
            }
        ],
        BaseDifficulty = 8
    };
}