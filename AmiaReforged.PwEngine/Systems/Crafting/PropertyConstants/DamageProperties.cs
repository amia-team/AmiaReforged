﻿using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class DamageProperties
{
    /// <summary>
    /// Generic one-handed weapon damage properties.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> Generic1HDamage = new[]
    {
        // +1 (minor)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Acid Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Cold Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Electrical Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Fire Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Negative Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Sonic Damage",
            CraftingTier = CraftingTier.Minor
        },

        // 1d4 (lesser)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Acid Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Cold Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Electrical Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Fire Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Negative Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Sonic Damage",
            CraftingTier = CraftingTier.Lesser
        },

        // 1d6 (intermediate)
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Acid Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Cold Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Electrical Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Fire Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Negative Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Sonic Damage",
            CraftingTier = CraftingTier.Intermediate
        },

        // 1d8 (greater)
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Acid Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Cold Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Electrical Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Fire Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Negative Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Sonic Damage",
            CraftingTier = CraftingTier.Greater
        }
    };

    /// <summary>
    /// Two-handed weapon damage properties.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> Generic2HProperties = new[]
    {
        // +1 (minor)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Acid Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Cold Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Electrical Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Fire Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Negative Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "+1 Sonic Damage",
            CraftingTier = CraftingTier.Minor
        },

        // 1d4 (lesser)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Acid Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Cold Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Electrical Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Fire Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Negative Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Sonic Damage",
            CraftingTier = CraftingTier.Lesser
        },

        // 1d6 (intermediate)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Acid Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Cold Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Electrical Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Fire Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Negative Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Sonic Damage",
            CraftingTier = CraftingTier.Intermediate
        },

        //2d4 (greater)
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
            GuiLabel = "2d4 Acid Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
            GuiLabel = "2d4 Cold Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
            GuiLabel = "2d4 Electrical Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
            GuiLabel = "2d4 Fire Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
            GuiLabel = "2d4 Negative Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
            GuiLabel = "2d4 Sonic Damage",
            CraftingTier = CraftingTier.Greater
        }
    };

    /// <summary>
    /// Gloves damage properties.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> GloveDamageProperties = new[]
    {
        // 1d2 (minor)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "1d2 Acid Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "1d2 Cold Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "1d2 Electrical Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "1d2 Fire Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "1d2 Negative Damage",
            CraftingTier = CraftingTier.Minor
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1)!,
            GuiLabel = "1d2 Sonic Damage",
            CraftingTier = CraftingTier.Minor
        },

        // 1d4 (lesser)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Acid Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Cold Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Electrical Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Fire Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Negative Damage",
            CraftingTier = CraftingTier.Lesser
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
            GuiLabel = "1d4 Sonic Damage",
            CraftingTier = CraftingTier.Lesser
        },

        // 1d6 (intermediate)
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Acid Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Cold Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Electrical Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Fire Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Negative Damage",
            CraftingTier = CraftingTier.Intermediate
        },
        new CraftingProperty
        {
            Cost = 1,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
            GuiLabel = "1d6 Sonic Damage",
            CraftingTier = CraftingTier.Intermediate
        },

        // 1d8 (greater)
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Acid Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Cold Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Electrical Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Fire Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Negative Damage",
            CraftingTier = CraftingTier.Greater
        },
        new CraftingProperty
        {
            Cost = 2,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
            GuiLabel = "1d8 Sonic Damage",
            CraftingTier = CraftingTier.Greater
        },

        // 2d6 (flawless)
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
            GuiLabel = "2d6 Acid Damage",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
            GuiLabel = "2d6 Cold Damage",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
            GuiLabel = "2d6 Electrical Damage",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
            GuiLabel = "2d6 Fire Damage",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
            GuiLabel = "2d6 Negative Damage",
            CraftingTier = CraftingTier.Flawless
        },
        new CraftingProperty
        {
            Cost = 3,
            Property = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
            GuiLabel = "2d6 Sonic Damage",
            CraftingTier = CraftingTier.Flawless
        }
    };
}