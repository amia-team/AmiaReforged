using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

public static class SpellResistanceProperties
{
    public static readonly CraftingCategory SpellResistances = new(categoryId: "spell_resistance")
    {
        Label = "Spell Resistance",
        Properties =
        [
            // Unattainable misc ones. 10, 12, 14, 16, 18 are not available to be crafted but only have 1 power cost.
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_10)!,
                GuiLabel = "Spell Resistance +10",
                PowerCost = 1,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_12)!,
                GuiLabel = "Spell Resistance +12",
                PowerCost = 1,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_14)!,
                GuiLabel = "Spell Resistance +14",
                PowerCost = 1,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_16)!,
                GuiLabel = "Spell Resistance +16",
                PowerCost = 1,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_18)!,
                GuiLabel = "Spell Resistance +18",
                PowerCost = 1,
                CraftingTier = CraftingTier.Unattainable
            },
            // Perfect
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_20)!,
                GuiLabel = "Spell Resistance +20",
                PowerCost = 2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_22)!,
                GuiLabel = "Spell Resistance +22",
                PowerCost = 2,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_24)!,
                GuiLabel = "Spell Resistance +24",
                PowerCost = 2,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_26)!,
                GuiLabel = "Spell Resistance +26",
                PowerCost = 2,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_28)!,
                GuiLabel = "Spell Resistance +28",
                PowerCost = 3,
                CraftingTier = CraftingTier.Unattainable
            },
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_30)!,
                GuiLabel = "Spell Resistance +30",
                PowerCost = 3,
                CraftingTier = CraftingTier.Unattainable
            },
            // Wondrous
            new CraftingProperty
            {
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_32)!,
                GuiLabel = "Spell Resistance +32",
                PowerCost = 4,
                CraftingTier = CraftingTier.Perfect
            }
        ]
    };
}
