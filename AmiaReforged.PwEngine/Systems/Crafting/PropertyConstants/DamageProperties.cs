using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class DamageProperties
{
    private const int MythalCost1D = 500;
    private const int MythalCost1D4 = 5000;
    private const int MythalCost1D6 = 20000;
    private const int MythalCost1D8 = 50000;
    private const int MythalCost2D4 = 100000;


    /// <summary>
    /// Generic one-handed weapon damage properties.
    /// </summary>
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
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            }
        },
        BaseDifficulty = 10
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
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },

            // 2d4 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Acid",
                GoldCost = MythalCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Cold",
                GoldCost = MythalCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Electrical",
                GoldCost = MythalCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Fire",
                GoldCost = MythalCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Negative",
                GoldCost = MythalCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Sonic",
                GoldCost = MythalCost2D4,
                CraftingTier = CraftingTier.Greater
            }
        },
        BaseDifficulty = 15
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
        },
        BaseDifficulty = 10
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
        },
        BaseDifficulty = 15
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
        },
        BaseDifficulty = 15
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
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                GoldCost = MythalCost1D,
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                GoldCost = MythalCost1D4,
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                GoldCost = MythalCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                GoldCost = MythalCost1D8,
                CraftingTier = CraftingTier.Greater
            }
        },
        BaseDifficulty = 10
    };
}