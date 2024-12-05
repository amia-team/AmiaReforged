using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CraftingPropertyData))]
public class CraftingPropertyData
{
    public Dictionary<int, IReadOnlyCollection<CraftingProperty>> Properties { get; } = new();

    public CraftingPropertyData()
    {
        SetupAmulets();
    }

    private void SetupAmulets()
    {
        // This list of properties is different because natural armor has its own unique costs.
        List<CraftingProperty> properties = new()
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(1)!,
                GuiLabel = "+1",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5",
                CraftingTier = CraftingTier.Flawless
            }
        };
        properties.AddRange(GenericItemProperties.ElementalResistances);
        properties.AddRange(GenericItemProperties.DamageReductions);
        Properties.Add(NWScript.BASE_ITEM_AMULET, properties);
    }
}

public static class GenericItemProperties
{
    /// <summary>
    ///     Generic elemental resistances.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> ElementalResistances = new[]
    {
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 5)!,
            GuiLabel = "5/- Acid Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 5)!,
            GuiLabel = "5/- Cold Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 5)!,
            GuiLabel = "5/- Electrical Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 5)!,
            GuiLabel = "5/- Fire Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 5)!,
            GuiLabel = "5/- Sonic Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_NEGATIVE, 5)!,
            GuiLabel = "5/- Negative Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 5)!,
            GuiLabel = "5/- Cold Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 5)!,
            GuiLabel = "5/- Electrical Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 5)!,
            GuiLabel = "5/- Fire Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 5)!,
            GuiLabel = "5/- Sonic Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_NEGATIVE, 5)!,
            GuiLabel = "5/- Negative Resistance",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 10)!,
            GuiLabel = "10/- Acid Resistance",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 10)!,
            GuiLabel = "10/- Cold Resistance",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 10)!,
            GuiLabel = "10/- Electrical Resistance",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 10)!,
            GuiLabel = "10/- Fire Resistance",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 10)!,
            GuiLabel = "10/- Sonic Resistance",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_NEGATIVE, 10)!,
            GuiLabel = "10/- Negative Resistance",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ACID, 15)!,
            GuiLabel = "15/- Acid Resistance",
            CraftingTier = CraftingTier.Divine
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_COLD, 15)!,
            GuiLabel = "15/- Cold Resistance",
            CraftingTier = CraftingTier.Divine
        }
    };

    /// <summary>
    ///     Generic damage soak properties.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> DamageReductions = new[]
    {
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageReduction(1, 5)!,
            GuiLabel = "+1 Soak 5 Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageReduction(2, 5)!,
            GuiLabel = "+2 Soak 5 Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageReduction(3, 5)!,
            GuiLabel = "+3 Soak 5 Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageReduction(4, 5)!,
            GuiLabel = "+4 Soak 5 Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageReduction(5, 5)!,
            GuiLabel = "+5 Soak 5 Damage",
            CraftingTier = CraftingTier.Flawless
        }
    };

    /// <summary>
    ///     Generic armor class properties.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> GenericArmor = new[]
    {
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyACBonus(1)!,
            GuiLabel = "+1",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyACBonus(2)!,
            GuiLabel = "+2",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyACBonus(3)!,
            GuiLabel = "+3",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyACBonus(4)!,
            GuiLabel = "+4",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 4,
            Property = NWScript.ItemPropertyACBonus(5)!,
            GuiLabel = "+5",
            CraftingTier = CraftingTier.Flawless
        }
    };

    public static readonly IReadOnlyList<CraftingProperty> GenericUniversalSaves = new[]
    {
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 1)!,
            GuiLabel = "+1 Universal Save",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 4,
            Property = NWScript.ItemPropertyBonusSavingThrow(NWScript.IP_CONST_SAVEVS_UNIVERSAL, 2)!,
            GuiLabel = "+2 Universal Save",
            CraftingTier = CraftingTier.Greater
        },
    };
    
}