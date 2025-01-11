using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class GenericItemProperties
{
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
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 5)!,
                GuiLabel = "5/- Cold",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 5)!,
                GuiLabel = "5/- Electrical",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 5)!,
                GuiLabel = "5/- Fire",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 5)!,
                GuiLabel = "5/- Sonic",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 10)!,
                GuiLabel = "10/- Acid",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 10)!,
                GuiLabel = "10/- Cold",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 10)!,
                GuiLabel = "10/- Electrical",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 10)!,
                GuiLabel = "10/- Fire",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 10)!,
                GuiLabel = "10/- Sonic",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 15)!,
                GuiLabel = "15/- Acid",
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 15)!,
                GuiLabel = "15/- Cold",
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 15)!,
                GuiLabel = "15/- Electrical",
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 15)!,
                GuiLabel = "15/- Fire",
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 15)!,
                GuiLabel = "15/- Sonic",
                CraftingTier = CraftingTier.Divine
            },
        }
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
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_PIERCING, 5)!,
                GuiLabel = "5/- Piercing",
                CraftingTier = CraftingTier.Divine
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SLASHING, 5)!,
                GuiLabel = "5/- Slashing",
                CraftingTier = CraftingTier.Divine
            },
        }
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
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(2, 5)!,
                GuiLabel = "+2 Soak 5 Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(3, 5)!,
                GuiLabel = "+3 Soak 5 Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageReduction(4, 5)!,
                GuiLabel = "+4 Soak 5 Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageReduction(5, 5)!,
                GuiLabel = "+5 Soak 5 Damage",
                CraftingTier = CraftingTier.Flawless
            }
        }
    };

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
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5",
                CraftingTier = CraftingTier.Flawless
            }
        }
    };

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
                CraftingTier = CraftingTier.Intermediate
            },
            // +2 (Greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Greater
            },
            // +3 (Flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyVampiricRegeneration(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Flawless
            },
        }
    };

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
                CraftingTier = CraftingTier.Intermediate
            },
            // Greater, +2 costs 4.
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyRegeneration(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Greater
            },
            // Flawless, +3 costs 6.
            new CraftingProperty
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyRegeneration(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Flawless
            },
        }
    };

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
                CraftingTier = CraftingTier.Perfect
            }
        }
    };
}