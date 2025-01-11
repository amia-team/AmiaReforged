using AmiaReforged.PwEngine.Systems.Crafting.Models;
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
                GuiLabel = "+1 Acid ",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
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
                GuiLabel = "+1 Acid",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                CraftingTier = CraftingTier.Intermediate
            },

            //2d4 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Acid",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Cold",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Electrical",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Fire",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Negative",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Sonic",
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
                GuiLabel = "1d2 Acid",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Cold",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Electrical",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Fire",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Negative",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Sonic",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                CraftingTier = CraftingTier.Greater
            },

            // 2d6 (flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Acid",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Cold",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Electrical",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Fire",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Negative",
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Sonic",
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
                GuiLabel = "+1 Acid",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                CraftingTier = CraftingTier.Greater
            }
        }
    };
}