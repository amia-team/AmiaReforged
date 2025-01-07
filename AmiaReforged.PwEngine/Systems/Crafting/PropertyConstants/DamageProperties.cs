﻿using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class DamageProperties
{
    /// <summary>
    /// Generic one-handed weapon damage properties.
    /// </summary>
    public static readonly CraftingCategory OneHanders = new("one_handers_damage")
    {
        Label = "Damage Bonus",
        // +1 (minor)
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Acid Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic Damage",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic Damage",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic Damage",
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic Damage",
                CraftingTier = CraftingTier.Greater
            }
        }
    };

    /// <summary>
    /// Two-handed weapon damage properties.
    /// </summary>
    public static readonly CraftingCategory TwoHanders = new("two_handers_damage")
    {
        Label = "Damage Bonus",
        // +1 (minor)
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Acid Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic Damage",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic Damage",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic Damage",
                CraftingTier = CraftingTier.Intermediate
            },

            //2d4 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Acid Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Cold Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Electrical Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Fire Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Negative Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Sonic Damage",
                CraftingTier = CraftingTier.Greater
            }
        }
    };

    /// <summary>
    /// Gloves damage properties.
    /// </summary>
    public static readonly CraftingCategory GloveDamage = new("glove_damage")
    {
        Label = "Damage Bonus",
        // 1d2 (minor)
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Acid Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Cold Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Electrical Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Fire Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Negative Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Sonic Damage",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic Damage",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic Damage",
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic Damage",
                CraftingTier = CraftingTier.Greater
            },

            // 2d6 (flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Acid Damage",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Cold Damage",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Electrical Damage",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Fire Damage",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Negative Damage",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Sonic Damage",
                CraftingTier = CraftingTier.Flawless
            }
        }
    };


    public static readonly CraftingCategory MassiveCriticals = new("massive_crits_damage")
    {
        Label = "Massive Criticals",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Massive Criticals",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Massive Criticals",
                CraftingTier = CraftingTier.Greater
            },
        }
    };

    /// <summary>
    /// See https://lexicon.nwn.wiki/index.php/ItemPropertyMaxRangeStrengthMod for more information.
    /// </summary>
    public static readonly CraftingCategory Mighty = new("mighty_damage")
    {
        Label = "Mighty",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(1)!,
                GuiLabel = "Mighty +1",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(2)!,
                GuiLabel = "Mighty +2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(3)!,
                GuiLabel = "+3 Mighty",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(4)!,
                GuiLabel = "+4 Mighty",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(5)!,
                GuiLabel = "+5 Mighty",
                CraftingTier = CraftingTier.Flawless
            }
        }
    };

    public static readonly CraftingCategory Ammo = new("ammo_damage")
    {
        Label = "Damage Bonus",
        // +1 (minor)
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Acid Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative Damage",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic Damage",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative Damage",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic Damage",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative Damage",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic Damage",
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative Damage",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic Damage",
                CraftingTier = CraftingTier.Greater
            }
        }
    };
}