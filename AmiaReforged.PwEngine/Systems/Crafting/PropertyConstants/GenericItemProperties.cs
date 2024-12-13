using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

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
        },
        new CraftingProperty
        {
        Cost = 3,
        Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 15)!,
        GuiLabel = "15/- Electrical Resistance",
        CraftingTier = CraftingTier.Divine
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 15)!,
            GuiLabel = "15/- Fire Resistance",
            CraftingTier = CraftingTier.Divine
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SONIC, 15)!,
            GuiLabel = "15/- Sonic Resistance",
            CraftingTier = CraftingTier.Divine
        },
    };

    public static readonly IReadOnlyList<CraftingProperty> PhysicalDamageResistances = new[]
    {
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_BLUDGEONING, 5)!,
            GuiLabel = "5/- Bludgeoning Resistance",
            CraftingTier = CraftingTier.Divine
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_PIERCING, 5)!,
            GuiLabel = "5/- Piercing Resistance",
            CraftingTier = CraftingTier.Divine
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageResistance(NWScript.DAMAGE_TYPE_SLASHING, 5)!,
            GuiLabel = "5/- Slashing Resistance",
            CraftingTier = CraftingTier.Divine
        },
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
    public static readonly IReadOnlyList<CraftingProperty> Armor = new[]
    {
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyACBonus(1)!,
            GuiLabel = "+1 AC",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyACBonus(2)!,
            GuiLabel = "+2 AC",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyACBonus(3)!,
            GuiLabel = "+3 AC",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyACBonus(4)!,
            GuiLabel = "+4 AC",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 4,
            Property = NWScript.ItemPropertyACBonus(5)!,
            GuiLabel = "+5 AC",
            CraftingTier = CraftingTier.Flawless
        }
    };

    /// <summary>
    /// Generic vampiric regeneration properties.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> VampiricRegeneration = new[]
    {
        // +1 (Intermediate)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyVampiricRegeneration(1)!,
            GuiLabel = "+1 Vampiric Regeneration",
            CraftingTier = CraftingTier.Intermediate
        },
        // +2 (Greater)
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyVampiricRegeneration(2)!,
            GuiLabel = "+2 Vampiric Regeneration",
            CraftingTier = CraftingTier.Greater
        },
        // +3 (Flawless)
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyVampiricRegeneration(3)!,
            GuiLabel = "+3 Vampiric Regeneration",
            CraftingTier = CraftingTier.Flawless
        },
    };

    public static readonly IReadOnlyList<CraftingProperty> Regeneration = new[]
    {
        // Intermediate, +1 Regeneration costs 2.
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyRegeneration(1)!,
            GuiLabel = "+1 Regeneration",
            CraftingTier = CraftingTier.Intermediate
        },
        // Greater, +2 Regeneration costs 4.
        new CraftingProperty
        {
            Cost = 4,
            Property = NWScript.ItemPropertyRegeneration(2)!,
            GuiLabel = "+2 Regeneration",
            CraftingTier = CraftingTier.Greater
        },
        // Flawless, +3 Regeneration costs 6.
        new CraftingProperty
        {
            Cost = 6,
            Property = NWScript.ItemPropertyRegeneration(3)!,
            GuiLabel = "+3 Regeneration",
            CraftingTier = CraftingTier.Flawless
        },
    };

    public static readonly CraftingProperty Keen = new CraftingProperty()
    {
        Cost = 1,
        Property = NWScript.ItemPropertyKeen()!,
        GuiLabel = "Keen",
        CraftingTier = CraftingTier.Perfect
    };

}