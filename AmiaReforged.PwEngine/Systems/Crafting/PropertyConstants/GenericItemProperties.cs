using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class GenericItemProperties
{
    private const int ResistanceCost1 = 10000;
    private const int ResistanceCost2 = 20000;
    private const int ResistanceCost3 = 30000;
    private const int ResistanceCost4 = 40000;
    private const int ResistanceCost5 = 50000;

    public static readonly CraftingCategory ElementalResistances = new("elemental_resists")
    {
        Label = "Elemental Resistances",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 5)!,
                GuiLabel = "5/- Acid",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 5)!,
                GuiLabel = "5/- Cold",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 5)!,
                GuiLabel = "5/- Electrical",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 5)!,
                GuiLabel = "5/- Fire",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 5)!,
                GuiLabel = "5/- Sonic",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 10)!,
                GuiLabel = "10/- Acid",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 10)!,
                GuiLabel = "10/- Cold",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 10)!,
                GuiLabel = "10/- Electrical",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 10)!,
                GuiLabel = "10/- Fire",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 10)!,
                GuiLabel = "10/- Sonic",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 15)!,
                GuiLabel = "15/- Acid",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 15)!,
                GuiLabel = "15/- Cold",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 15)!,
                GuiLabel = "15/- Electrical",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 15)!,
                GuiLabel = "15/- Fire",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 15)!,
                GuiLabel = "15/- Sonic",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
        },
        BaseDifficulty = 13
    };

    /// <summary>
    /// Physical damage resistance category
    /// </summary>
    public static readonly CraftingCategory PhysicalDamageResistances = new("physical_resists")
    {
        Label = "Physical Damage Resistance",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_BLUDGEONING, 5)!,
                GuiLabel = "5/- Bludgeoning",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_PIERCING, 5)!,
                GuiLabel = "5/- Piercing",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SLASHING, 5)!,
                GuiLabel = "5/- Slashing",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Divine
            },
        },
        BaseDifficulty = 18
    };


    /// <summary>
    /// Damage reduction category
    /// </summary>
    public static readonly CraftingCategory DamageReductions = new("damage_reduction")
    {
        Label = "Damage Reduction",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(1, 5)!,
                GuiLabel = "+1 Soak 5 Damage",
                GoldCost = ResistanceCost1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(2, 5)!,
                GuiLabel = "+2 Soak 5 Damage",
                GoldCost = ResistanceCost2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(3, 5)!,
                GuiLabel = "+3 Soak 5 Damage",
                GoldCost = ResistanceCost3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(4, 5)!,
                GuiLabel = "+4 Soak 5 Damage",
                GoldCost = ResistanceCost4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageReduction(5, 5)!,
                GuiLabel = "+5 Soak 5 Damage",
                GoldCost = ResistanceCost5,
                CraftingTier = CraftingTier.Flawless
            }
        },
        PerformValidation = (_, i) =>
            NWScript.GetItemHasItemProperty(i, NWScript.ITEM_PROPERTY_DAMAGE_REDUCTION) == NWScript.TRUE
                ? PropertyValidationResult.BasePropertyMustBeUnique
                : PropertyValidationResult.Valid,
        BaseDifficulty = 18
    };

    private const int AcCost1 = 2000;
    private const int AcCost2 = 6000;
    private const int AcCost3 = 12000;
    private const int AcCost4 = 20000;
    private const int AcCost5 = 30000;

    public static readonly CraftingCategory Armor = new("armor")
    {
        Label = "Armor Class",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyACBonus(1)!,
                GuiLabel = "+1",
                GoldCost = AcCost1,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2",
                GoldCost = AcCost2,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3",
                GoldCost = AcCost3,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4",
                GoldCost = AcCost4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5",
                GoldCost = AcCost5,
                CraftingTier = CraftingTier.Flawless
            }
        },
        PerformValidation = (_, item) =>
            NWScript.GetItemHasItemProperty(item, NWScript.ITEM_PROPERTY_AC_BONUS) == NWScript.TRUE
                ? PropertyValidationResult.BasePropertyMustBeUnique
                : PropertyValidationResult.Valid,
        BaseDifficulty = 9
    };

    private const int MythalCostVregen1 = 2000;
    private const int MythalCostVregen2 = 15000;
    private const int MythalCostVregen3 = 75000;

    /// <summary>
    /// Vampiric regeneration category.
    /// </summary>
    public static readonly CraftingCategory VampiricRegeneration = new("vampiric_regeneration")
    {
        Label = "Vampiric Regeneration",
        Properties = new[]
        {
            // +1 (Intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(1)!,
                GuiLabel = "+1",
                GoldCost = MythalCostVregen1,
                CraftingTier = CraftingTier.Intermediate
            },
            // +2 (Greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(2)!,
                GuiLabel = "+2",
                GoldCost = MythalCostVregen2,
                CraftingTier = CraftingTier.Greater
            },
            // +3 (Flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(3)!,
                GuiLabel = "+3",
                GoldCost = MythalCostVregen3,
                CraftingTier = CraftingTier.Flawless
            },
        },
        BaseDifficulty = 10
    };

    private const int MythalCostRegen1 = 20000;
    private const int MythalCostRegen2 = 30000;
    private const int MythalCostRegen3 = 50000;

    public static readonly CraftingCategory Regeneration = new("regeneration")
    {
        Label = "Regeneration",
        Properties = new[]
        {
            // Intermediate, +1 Regeneration costs 2.
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyRegeneration(1)!,
                GuiLabel = "+1",
                GoldCost = MythalCostRegen1,
                CraftingTier = CraftingTier.Intermediate
            },
            // Greater, +2 costs 4.
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyRegeneration(2)!,
                GuiLabel = "+2",
                GoldCost = MythalCostRegen2,
                CraftingTier = CraftingTier.Greater
            },
            // Flawless, +3 costs 6.
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyRegeneration(3)!,
                GuiLabel = "+3",
                GoldCost = MythalCostRegen3,
                CraftingTier = CraftingTier.Flawless
            },
        },
        PerformValidation = (_, item) =>
            NWScript.GetItemHasItemProperty(item, NWScript.ITEM_PROPERTY_REGENERATION) == NWScript.TRUE
                ? PropertyValidationResult.BasePropertyMustBeUnique
                : PropertyValidationResult.Valid,
        BaseDifficulty = 6
    };

    private const int MythalKeenCost = 50000;

    public static readonly CraftingCategory Other = new("others")
    {
        Label = "Other Properties",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyKeen()!,
                GuiLabel = "Keen",
                GoldCost = MythalKeenCost,
                CraftingTier = CraftingTier.Perfect
            }
        },
        BaseDifficulty = 15
    };
}