using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

/// <summary>
/// Wondrous tier properties that can only be added by DMs through the Mythal Forge.
/// These properties have varying power costs and represent special or unique item enhancements.
/// </summary>
public static class WondrousDmProperties
{
    public static readonly CraftingCategory ArcaneSpellFailureReduction = new(categoryId: "arcane_spell_failure_reduction")
    {
        Label = "Arcane Spell Failure Reduction",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyArcaneSpellFailure(NWScript.IP_CONST_ARCANE_SPELL_FAILURE_MINUS_50_PERCENT)!,
                GuiLabel = "Arcane Spell Failure -50%",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyArcaneSpellFailure(NWScript.IP_CONST_ARCANE_SPELL_FAILURE_MINUS_45_PERCENT)!,
                GuiLabel = "Arcane Spell Failure -45%",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyArcaneSpellFailure(NWScript.IP_CONST_ARCANE_SPELL_FAILURE_MINUS_20_PERCENT)!,
                GuiLabel = "Arcane Spell Failure -20%",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyArcaneSpellFailure(NWScript.IP_CONST_ARCANE_SPELL_FAILURE_MINUS_15_PERCENT)!,
                GuiLabel = "Arcane Spell Failure -15%",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyArcaneSpellFailure(NWScript.IP_CONST_ARCANE_SPELL_FAILURE_MINUS_5_PERCENT)!,
                GuiLabel = "Arcane Spell Failure -5%",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory SneakAttackFeats = new(categoryId: "sneak_attack_feats")
    {
        Label = "Sneak Attack Feats",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SNEAK_ATTACK_1D6)!,
                GuiLabel = "Sneak Attack 1d6",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(440)!,  // Blackguard Sneak Attack 1d6 feat index from iprp_feats.2da
                GuiLabel = "Blackguard Sneak Attack 1d6",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory ImmunityMiscellaneous = new(categoryId: "immunity_miscellaneous")
    {
        Label = "Immunity (Miscellaneous)",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyImmunityMisc(NWScript.IP_CONST_IMMUNITYMISC_DISEASE)!,
                GuiLabel = "Immunity: Disease",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyImmunityMisc(NWScript.IP_CONST_IMMUNITYMISC_POISON)!,
                GuiLabel = "Immunity: Poison",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(251)!,  // Immunity to Sleep feat index from iprp_feats.2da
                GuiLabel = "Immunity: Sleep",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory BonusFeats = new(categoryId: "bonus_feats_wondrous")
    {
        Label = "Bonus Feats (Wondrous)",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusFeat(226)!,  // Evasion feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Evasion",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyFreeAction()!,
                GuiLabel = "Bonus Feat: Freedom",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_ALERTNESS)!,
                GuiLabel = "Bonus Feat: Alertness",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(363)!,  // Artist feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Artist",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusFeat(63)!,  // Called Shot feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Called Shot",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_DODGE)!,
                GuiLabel = "Bonus Feat: Dodge",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_POINTBLANK)!,
                GuiLabel = "Bonus Feat: Point Blank Shot",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(393)!,  // Rapid Reload feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Rapid Reload",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_RAPID_SHOT)!,
                GuiLabel = "Bonus Feat: Rapid Shot",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSABJ)!,
                GuiLabel = "Bonus Feat: Spell Focus (Abjuration)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSCON)!,
                GuiLabel = "Bonus Feat: Spell Focus (Conjuration)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSDIV)!,
                GuiLabel = "Bonus Feat: Spell Focus (Divination)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSENC)!,
                GuiLabel = "Bonus Feat: Spell Focus (Enchantment)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSEVO)!,
                GuiLabel = "Bonus Feat: Spell Focus (Evocation)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSILL)!,
                GuiLabel = "Bonus Feat: Spell Focus (Illusion)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_SPELLFOCUSNEC)!,
                GuiLabel = "Bonus Feat: Spell Focus (Necromancy)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_SPELLSCHOOL_TRANSMUTATION)!,
                GuiLabel = "Bonus Feat: Spell Focus (Transmutation)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusFeat(386)!,  // Thug feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Thug",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(371)!,  // Stealthy feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Stealthy",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_WEAPON_PROF_EXOTIC)!,
                GuiLabel = "Bonus Feat: Weapon Proficiency (Exotic)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_WEAPON_PROF_MARTIAL)!,
                GuiLabel = "Bonus Feat: Weapon Proficiency (Martial)",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(NWScript.IP_CONST_FEAT_COMBAT_CASTING)!,
                GuiLabel = "Bonus Feat: Combat Casting",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDarkvision()!,
                GuiLabel = "Bonus Feat: Darkvision",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusFeat(405)!,  // Extra Music feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Extra Music",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusFeat(359)!,  // Low-Light Vision feat index from iprp_feats.2da
                GuiLabel = "Bonus Feat: Low-Light Vision",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory SpellResistanceWondrous = new(categoryId: "spell_resistance_wondrous")
    {
        Label = "Spell Resistance (Wondrous)",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_32)!,
                GuiLabel = "Spell Resistance: 32",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyBonusSpellResistance(NWScript.IP_CONST_SPELLRESISTANCEBONUS_20)!,
                GuiLabel = "Spell Resistance: 20",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };

    public static readonly CraftingCategory WeightReduction = new(categoryId: "weight_reduction")
    {
        Label = "Weight Reduction",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyWeightReduction(NWScript.IP_CONST_REDUCEDWEIGHT_80_PERCENT)!,
                GuiLabel = "Weight Reduction: 80% of Weight",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyWeightReduction(NWScript.IP_CONST_REDUCEDWEIGHT_60_PERCENT)!,
                GuiLabel = "Weight Reduction: 60% of Weight",
                GoldCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                Removable = true
            }
        ],
        BaseDifficulty = 0
    };
}

