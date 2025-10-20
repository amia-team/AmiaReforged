using AmiaReforged.PwEngine.Features.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

public static class GenericItemProperties
{
    private const int ResistanceCost1 = 10000;
    private const int ResistanceCost2 = 20000;
    private const int ResistanceCost3 = 30000;
    private const int ResistanceCost4 = 40000;
    private const int ResistanceCost5 = 50000;

    public const int AcCost1 = 2000;
    public const int AcCost2 = 6000;
    public const int AcCost3 = 12000;
    public const int AcCost4 = 20000;
    public const int AcCost5 = 30000;

    private const int MythalCostVregen1 = 2000;
    private const int MythalCostVregen2 = 15000;
    private const int MythalCostVregen3 = 75000;

    private const int MythalCostRegen1 = 20000;
    private const int MythalCostRegen2 = 30000;
    private const int MythalCostRegen3 = 50000;

    private const int MythalKeenCost = 50000;

    public static readonly CraftingCategory ElementalResistances = new(categoryId: "elemental_resists")
    {
        Label = "Elemental Resistances",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_ACID,
                        NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Acid",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_COLD,
                        NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Cold",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Electrical",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                        NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Fire",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                        NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Sonic",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_ACID,
                        NWScript.IP_CONST_DAMAGERESIST_10)!,
                GuiLabel = "10/- Acid",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_COLD,
                        NWScript.IP_CONST_DAMAGERESIST_10)!,
                GuiLabel = "10/- Cold",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGERESIST_10)!,
                GuiLabel = "10/- Electrical",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                        NWScript.IP_CONST_DAMAGERESIST_10)!,
                GuiLabel = "10/- Fire",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                        NWScript.IP_CONST_DAMAGERESIST_10)!,
                GuiLabel = "10/- Sonic",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_ACID,
                        NWScript.IP_CONST_DAMAGERESIST_15)!,
                GuiLabel = "15/- Acid",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_COLD,
                        NWScript.IP_CONST_DAMAGERESIST_15)!,
                GuiLabel = "15/- Cold",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGERESIST_15)!,
                GuiLabel = "15/- Electrical",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                        NWScript.IP_CONST_DAMAGERESIST_15)!,
                GuiLabel = "15/- Fire",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                        NWScript.IP_CONST_DAMAGERESIST_15)!,
                GuiLabel = "15/- Sonic",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            }
        ],
        BaseDifficulty = 13
    };

    /// <summary>
    ///     Physical damage resistance category
    /// </summary>
    public static readonly CraftingCategory PhysicalDamageResistances = new(categoryId: "physical_resists")
    {
        Label = "Physical Damage Resistance",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_BLUDGEONING,
                    NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Bludgeoning",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_PIERCING,
                        NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Piercing",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty =
                    NWScript.ItemPropertyDamageResistance(NWScript.IP_CONST_DAMAGETYPE_SLASHING,
                        NWScript.IP_CONST_DAMAGERESIST_5)!,
                GuiLabel = "5/- Slashing",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            }
        ],
        BaseDifficulty = 18
    };


    /// <summary>
    ///     Damage reduction category
    /// </summary>
    public static readonly CraftingCategory DamageReductions = new(categoryId: "damage_reduction")
    {
        Label = "Damage Reduction",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(1, NWScript.IP_CONST_DAMAGESOAK_5_HP)!,
                GuiLabel = "+1 Soak 5 Damage",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(2, NWScript.IP_CONST_DAMAGESOAK_5_HP)!,
                GuiLabel = "+2 Soak 5 Damage",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(3, NWScript.IP_CONST_DAMAGESOAK_5_HP)!,
                GuiLabel = "+3 Soak 5 Damage",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(4, NWScript.IP_CONST_DAMAGESOAK_5_HP)!,
                GuiLabel = "+4 Soak 5 Damage",
                GoldCost = ResistanceCost4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageReduction(5, NWScript.IP_CONST_DAMAGESOAK_5_HP)!,
                GuiLabel = "+5 Soak 5 Damage",
                GoldCost = ResistanceCost5,
                CraftingTier = CraftingTier.Flawless
            }
        ],
        BaseDifficulty = 18
    };

    public static readonly CraftingCategory Armor = new(categoryId: "armor")
    {
        Label = "Armor Class",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyACBonus(1)!,
                GuiLabel = "+1 AC",
                GoldCost = AcCost1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2 AC",
                GoldCost = AcCost2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3 AC",
                GoldCost = AcCost3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4 AC",
                GoldCost = AcCost4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5 AC",
                GoldCost = AcCost5,
                CraftingTier = CraftingTier.Flawless
            }
        ],
        BaseDifficulty = 9
    };

    /// <summary>
    ///     Vampiric regeneration category.
    /// </summary>
    public static readonly CraftingCategory VampiricRegeneration = new(categoryId: "vampiric_regeneration")
    {
        Label = "Vampiric Regeneration",
        Properties =
        [
            // +1 (Intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(1)!,
                GuiLabel = "+1 Vampiric Regeneration",
                GoldCost = MythalCostVregen1,
                CraftingTier = CraftingTier.Intermediate
            },
            // +2 (Greater)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(2)!,
                GuiLabel = "+2 Vampiric Regeneration",
                GoldCost = MythalCostVregen2,
                CraftingTier = CraftingTier.Greater
            },
            // +3 (Flawless)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(3)!,
                GuiLabel = "+3 Vampiric Regeneration",
                GoldCost = MythalCostVregen3,
                CraftingTier = CraftingTier.Flawless
            }
        ],
        BaseDifficulty = 10
    };

    public static readonly CraftingCategory Regeneration = new(categoryId: "regeneration")
    {
        Label = "Regeneration",
        Properties =
        [
            // Intermediate, +1 Regeneration costs 2.
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyRegeneration(1)!,
                GuiLabel = "+1 Regeneration",
                GoldCost = MythalCostRegen1,
                CraftingTier = CraftingTier.Intermediate
            },
            // Greater, +2 costs 4.
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyRegeneration(2)!,
                GuiLabel = "+2 Regeneration",
                GoldCost = MythalCostRegen2,
                CraftingTier = CraftingTier.Greater
            },
            // Flawless, +3 costs 6.
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyRegeneration(3)!,
                GuiLabel = "+3 Regeneration",
                GoldCost = MythalCostRegen3,
                CraftingTier = CraftingTier.Wondrous
            },
            // Unattainable, +4 costs 8.
            new CraftingProperty
            {
                PowerCost = 8,
                ItemProperty = NWScript.ItemPropertyRegeneration(4)!,
                GuiLabel = "+4 Regeneration",
                GoldCost = MythalCostRegen3,
                CraftingTier = CraftingTier.Wondrous
            }
        ],
        BaseDifficulty = 6
    };

    public static readonly CraftingCategory Keen = new(categoryId: "keen")
    {
        Label = "Keen",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyKeen()!,
                GuiLabel = "Keen",
                GoldCost = MythalKeenCost,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        BaseDifficulty = 15
    };


    public static readonly CraftingCategory KeenThrown = new(categoryId: "keen_thrown")
    {
        Label = "Keen",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyKeen()!,
                GuiLabel = "Keen",
                GoldCost = MythalKeenCost,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        BaseDifficulty = 15
    };
}
