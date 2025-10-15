using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

public static class SavingThrowProperties
{
    private const int SpecificSaveCost1 = 1000;
    private const int SpecificSaveCost2 = 5000;
    private const int SpecificSaveCost3 = 15000;
    private const int SpecificSaveCost4 = 30000;

    private const int UniversalSaveCost1 = 10000;
    private const int UniversalSaveCost2 = 15000;
    private const int UniversalSaveCost3 = 30000;
    private const int UniversalSaveCost4 = 50000;

    private const int GeneralSaveCost1 = 1000;
    private const int GeneralSaveCost2 = 5000;
    private const int GeneralSaveCost3 = 15000;
    private const int GeneralSaveCost4 = 30000;
    private const int GeneralSaveCost5 = 50000;
    private const int GeneralSaveCost6 = 75000;

    /// <summary>
    ///     Specific Saves for Perfect mythals.
    /// </summary>
    public static readonly CraftingCategory SpecificSaves = new(categoryId: "specific_saves")
    {
        Label = "Save vs Specific",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ACID, 6)!,
                GuiLabel = "+6 vs Acid",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_COLD, 6)!,
                GuiLabel = "+6 vs Cold",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ELECTRICAL, 6)!,
                GuiLabel = "+6 vs Electrical",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FIRE, 6)!,
                GuiLabel = "+6 vs Fire",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_SONIC, 6)!,
                GuiLabel = "+6 vs Sonic",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_NEGATIVE, 6)!,
                GuiLabel = "+6 vs Negative",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POISON, 6)!,
                GuiLabel = "+6 vs Poison",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POSITIVE, 6)!,
                GuiLabel = "+6 vs Positive",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FEAR, 6)!,
                GuiLabel = "+6 vs Fear",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DISEASE, 6)!,
                GuiLabel = "+6 vs Disease",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DIVINE, 6)!,
                GuiLabel = "+6 vs Divine",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_MINDAFFECTING, 6)!,
                GuiLabel = "+6 vs Mind-Affecting",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DEATH, 6)!,
                GuiLabel = "+6 vs Death",
                GoldCost = SpecificSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ACID, 3)!,
                GuiLabel = "+3 vs Acid",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_COLD, 3)!,
                GuiLabel = "+3 vs Cold",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ELECTRICAL, 3)!,
                GuiLabel = "+3 vs Electrical",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FIRE, 3)!,
                GuiLabel = "+3 vs Fire",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_SONIC, 3)!,
                GuiLabel = "+3 vs Sonic",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_NEGATIVE, 3)!,
                GuiLabel = "+3 vs Negative",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POISON, 3)!,
                GuiLabel = "+3 vs Poison",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POSITIVE, 3)!,
                GuiLabel = "+3 vs Positive",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FEAR, 3)!,
                GuiLabel = "+3 vs Fear",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DISEASE, 3)!,
                GuiLabel = "+3 vs Disease",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DIVINE, 3)!,
                GuiLabel = "+3 vs Divine",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_MINDAFFECTING, 3)!,
                GuiLabel = "+3 vs Mind-Affecting",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DEATH, 3)!,
                GuiLabel = "+3 vs Death",
                GoldCost = SpecificSaveCost2,
                CraftingTier = CraftingTier.Intermediate
            }
        ],
        BaseDifficulty = 15
    };

    public static readonly CraftingCategory UniversalSaves = new(categoryId: "universal_saves")
    {
        Label = "Universal Saves",
        Properties =
        [
            // Universal saves...
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 1)!,
                GuiLabel = "+1 Universal",
                GoldCost = UniversalSaveCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 2)!,
                GuiLabel = "+2 Universal",
                GoldCost = UniversalSaveCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 3)!,
                GuiLabel = "+3 Universal",
                GoldCost = UniversalSaveCost3,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 4)!,
                GuiLabel = "+4 Universal",
                GoldCost = UniversalSaveCost4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 10,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 5)!,
                GuiLabel = "+5 Universal",
                CraftingTier = CraftingTier.Wondrous
            },
            new CraftingProperty
            {
                PowerCost = 12,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 6)!,
                GuiLabel = "+6 Universal",
                CraftingTier = CraftingTier.Wondrous
            }
        ],
        BaseDifficulty = 10
    };

    public static readonly CraftingCategory GeneralSaves = new(categoryId: "general_saves")
    {
        Label = "Saving Throws",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_WILL, 6)!,
                GuiLabel = "+6 Will",
                GoldCost = GeneralSaveCost5,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_FORTITUDE, 6)!,
                GuiLabel = "+6 Fortitude",
                GoldCost = GeneralSaveCost5,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_REFLEX, 6)!,
                GuiLabel = "+6 Reflex",
                GoldCost = GeneralSaveCost5,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_WILL, 3)!,
                GuiLabel = "+3 Will",
                GoldCost = GeneralSaveCost3,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_FORTITUDE, 3)!,
                GuiLabel = "+3 Fortitude",
                GoldCost = GeneralSaveCost3,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_REFLEX, 3)!,
                GuiLabel = "+3 Reflex",
                GoldCost = GeneralSaveCost3,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_WILL, 1)!,
                GuiLabel = "+1 Will",
                GoldCost = GeneralSaveCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_FORTITUDE, 1)!,
                GuiLabel = "+1 Fortitude",
                GoldCost = GeneralSaveCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_REFLEX, 1)!,
                GuiLabel = "+1 Reflex",
                GoldCost = GeneralSaveCost1,
                CraftingTier = CraftingTier.Intermediate
            }
        ],
        BaseDifficulty = 8
    };
}
