using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class SavingThrowProperties
{
    /// <summary>
    /// Specific Saves for Perfect mythals.
    /// </summary>
    public static readonly CraftingCategory SpecificSaves = new("specific_saves")
    {
        Label = "Save vs Specific",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ACID, 6)!,
                GuiLabel = "+6 vs Acid",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_COLD, 6)!,
                GuiLabel = "+6 vs Cold",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ELECTRICAL, 6)!,
                GuiLabel = "+6 vs Electrical",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FIRE, 6)!,
                GuiLabel = "+6 vs Fire",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_SONIC, 6)!,
                GuiLabel = "+6 vs Sonic",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_NEGATIVE, 6)!,
                GuiLabel = "+6 vs Negative",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POISON, 6)!,
                GuiLabel = "+6 vs Poison",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POSITIVE, 6)!,
                GuiLabel = "+6 vs Positive",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FEAR, 6)!,
                GuiLabel = "+6 vs Fear",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DISEASE, 6)!,
                GuiLabel = "+6 vs Disease",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DIVINE, 6)!,
                GuiLabel = "+6 vs Divine",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_MINDAFFECTING, 6)!,
                GuiLabel = "+6 vs Mind-Affecting",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DEATH, 6)!,
                GuiLabel = "+6 vs Death",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ACID, 3)!,
                GuiLabel = "+3 vs Acid",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_COLD, 3)!,
                GuiLabel = "+3 vs Cold",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_ELECTRICAL, 3)!,
                GuiLabel = "+3 vs Electrical",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FIRE, 3)!,
                GuiLabel = "+3 vs Fire",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_SONIC, 3)!,
                GuiLabel = "+3 vs Sonic",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_NEGATIVE, 3)!,
                GuiLabel = "+3 vs Negative",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POISON, 3)!,
                GuiLabel = "+3 vs Poison",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_POSITIVE, 3)!,
                GuiLabel = "+3 vs Positive",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_FEAR, 3)!,
                GuiLabel = "+3 vs Fear",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DISEASE, 3)!,
                GuiLabel = "+3 vs Disease",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DIVINE, 3)!,
                GuiLabel = "+3 vs Divine",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_MINDAFFECTING, 3)!,
                GuiLabel = "+3 vs Mind-Affecting",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_DEATH, 3)!,
                GuiLabel = "+3 vs Death",
                CraftingTier = CraftingTier.Perfect
            },
            // Universal saves...
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 1)!,
                GuiLabel = "+1 Universal",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 2)!,
                GuiLabel = "+2 Universal",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 6,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 3)!,
                GuiLabel = "+3 Universal",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                Cost = 8,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 4)!,
                GuiLabel = "+4 Universal",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 10,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 5)!,
                GuiLabel = "+5 Universal",
                CraftingTier = CraftingTier.DreamCoin
            },
            new CraftingProperty
            {
                Cost = 12,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrowVsX(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 6)!,
                GuiLabel = "+6 Universal",
                CraftingTier = CraftingTier.DreamCoin
            },
        }
    };

    public static readonly CraftingCategory GeneralSaves = new("general_saves")
    {
        Label = "Saving Throws",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_WILL, 6)!,
                GuiLabel = "+6 Will",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_FORTITUDE, 6)!,
                GuiLabel = "+6 Fortitude",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_REFLEX, 6)!,
                GuiLabel = "+6 Reflex",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_WILL, 3)!,
                GuiLabel = "+3 Will",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_FORTITUDE, 3)!,
                GuiLabel = "+3 Fortitude",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_REFLEX, 3)!,
                GuiLabel = "+3 Reflex",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_WILL, 1)!,
                GuiLabel = "+1 Will",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_FORTITUDE, 1)!,
                GuiLabel = "+1 Fortitude",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                ItemProperty = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEBASETYPE_REFLEX, 1)!,
                GuiLabel = "+1 Reflex",
                CraftingTier = CraftingTier.Minor
            },
        }
    };
}