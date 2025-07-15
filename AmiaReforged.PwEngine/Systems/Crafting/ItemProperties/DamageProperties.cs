using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public static class DamageProperties
{
    private const int MeleeGoldCost1Damage = 500;
    private const int MeleeGoldCost1D4 = 5000;
    private const int MeleeGoldCost1D6 = 20000;
    private const int MeleeGoldCost1D8 = 50000;
    private const int MeleeGoldCost1D10 = 100000;
    private const int MeleeGoldCost2D4 = 100000;
    private const int MeleeGoldCost2D6 = 150000;

    private const int GloveGoldCost1Damage = 1000;
    private const int GloveGoldCost1d4 = 5000;
    private const int GloveGoldCost1d6 = 10000;
    private const int GloveGoldCost4 = 20000;
    private const int GloveGoldCost5 = 50000;


    /// <summary>
    ///     Generic one-handed weapon damage properties.
    /// </summary>
    /// <summary>
    ///     Generic one-handed weapon damage properties.
    /// </summary>
    public static readonly CraftingCategory OneHanders = new(categoryId: "one_handers_damage")
    {
        Label = "Damage Bonus",
        // +1 (minor)
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Acid ",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            
            // 1d10 (Flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Acid",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Cold",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Electrical",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Fire",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Negative",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Sonic",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_BLUDGEONING,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Bludgeoning",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_PIERCING,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Piercing",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SLASHING,
                    NWScript.IP_CONST_DAMAGEBONUS_1d10)!,
                GuiLabel = "1d10 Slashing",
                GoldCost = MeleeGoldCost1D10,
                CraftingTier = CraftingTier.Flawless
            },
        ],
        BaseDifficulty = 10
    };

    /// <summary>
    ///     Two-handed weapon damage properties.
    /// </summary>
    public static readonly CraftingCategory TwoHanders = new(categoryId: "two_handers_damage")
    {
        Label = "Damage Bonus",
        // +1 (minor)
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Acid",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },

            // 2d4 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Acid",
                GoldCost = MeleeGoldCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Cold",
                GoldCost = MeleeGoldCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Electrical",
                GoldCost = MeleeGoldCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Fire",
                GoldCost = MeleeGoldCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Negative",
                GoldCost = MeleeGoldCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d4)!,
                GuiLabel = "2d4 Sonic",
                GoldCost = MeleeGoldCost2D4,
                CraftingTier = CraftingTier.Greater
            },
            
            // 2d6 (Flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Acid",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Cold",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Electrical",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Fire",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Negative",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Sonic",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_BLUDGEONING,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Bludgeoning",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_PIERCING,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Piercing",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SLASHING,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Slashing",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = MeleeGoldCost2D6
            },
        ],
        BaseDifficulty = 15
    };

    /// <summary>
    ///     Gloves damage properties.
    /// </summary>
    public static readonly CraftingCategory GloveDamage = new(categoryId: "glove_damage")
    {
        Label = "Damage Bonus",
        // 1d2 (minor)
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Acid",
                CraftingTier = CraftingTier.Minor,
                GoldCost = GloveGoldCost1Damage
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Cold",
                CraftingTier = CraftingTier.Minor,
                GoldCost = GloveGoldCost1Damage
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
                CraftingTier = CraftingTier.Minor,
                GoldCost = GloveGoldCost1Damage
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Negative",
                CraftingTier = CraftingTier.Minor,
                GoldCost = GloveGoldCost1Damage
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "1d2 Sonic",
                CraftingTier = CraftingTier.Minor,
                GoldCost = GloveGoldCost1Damage
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                CraftingTier = CraftingTier.Lesser,
                GoldCost = GloveGoldCost1d4
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                CraftingTier = CraftingTier.Lesser,
                GoldCost = GloveGoldCost1d4
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                CraftingTier = CraftingTier.Lesser,
                GoldCost = GloveGoldCost1d4
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                CraftingTier = CraftingTier.Lesser,
                GoldCost = GloveGoldCost1d4
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                CraftingTier = CraftingTier.Lesser,
                GoldCost = GloveGoldCost1d4
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                CraftingTier = CraftingTier.Lesser,
                GoldCost = GloveGoldCost1d4
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = GloveGoldCost1d6
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = GloveGoldCost1d6
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = GloveGoldCost1d6
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = GloveGoldCost1d6
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = GloveGoldCost1d6
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = GloveGoldCost1d6
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                CraftingTier = CraftingTier.Greater,
                GoldCost = GloveGoldCost4
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                CraftingTier = CraftingTier.Greater,
                GoldCost = GloveGoldCost4
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                CraftingTier = CraftingTier.Greater,
                GoldCost = GloveGoldCost4
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                CraftingTier = CraftingTier.Greater,
                GoldCost = GloveGoldCost4
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                CraftingTier = CraftingTier.Greater,
                GoldCost = GloveGoldCost4
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                CraftingTier = CraftingTier.Greater,
                GoldCost = GloveGoldCost4
            },

            // 2d6 (flawless)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Acid",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Cold",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Electrical",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Fire",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Negative",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Sonic",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_BLUDGEONING,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Bludgeoning",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_PIERCING,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Piercing",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SLASHING,
                    NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Slashing",
                CraftingTier = CraftingTier.Flawless,
                GoldCost = GloveGoldCost5
            },
        ],
        BaseDifficulty = 10
    };


    public static readonly CraftingCategory MassiveCriticals = new(categoryId: "massive_crits_damage")
    {
        Label = "Massive Criticals",
        Properties =
        [
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
            }
        ],
        BaseDifficulty = 15
    };

    /// <summary>
    ///     See https://lexicon.nwn.wiki/index.php/ItemPropertyMaxRangeStrengthMod for more information.
    /// </summary>
    public static readonly CraftingCategory Mighty = new(categoryId: "mighty_damage")
    {
        Label = "Mighty",
        Properties =
        [
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
                GuiLabel = "Mighty +3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(4)!,
                GuiLabel = "Mighty +4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyMaxRangeStrengthMod(5)!,
                GuiLabel = "Mighty +5",
                CraftingTier = CraftingTier.Flawless
            }
        ],
        BaseDifficulty = 15
    };

    public static readonly CraftingCategory Ammo = new(categoryId: "ammo_damage")
    {
        Label = "Damage Bonus",
        // +1 (minor)
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Acid",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Cold",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Electrical",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Fire",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Negative",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1)!,
                GuiLabel = "+1 Sonic",
                GoldCost = MeleeGoldCost1Damage,
                CraftingTier = CraftingTier.Minor
            },

            // 1d4 (lesser)
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Acid",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Cold",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Electrical",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Fire",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Negative",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d4)!,
                GuiLabel = "1d4 Sonic",
                GoldCost = MeleeGoldCost1D4,
                CraftingTier = CraftingTier.Lesser
            },

            // 1d6 (intermediate)
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Acid",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Cold",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Electrical",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Fire",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Negative",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d6)!,
                GuiLabel = "1d6 Sonic",
                GoldCost = MeleeGoldCost1D6,
                CraftingTier = CraftingTier.Intermediate
            },

            // 1d8 (greater)
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ACID,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Acid",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_COLD,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Cold",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_ELECTRICAL,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Electrical",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_FIRE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Fire",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_NEGATIVE,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Negative",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 3,
                ItemProperty = NWScript.ItemPropertyDamageBonus(NWScript.IP_CONST_DAMAGETYPE_SONIC,
                    NWScript.IP_CONST_DAMAGEBONUS_1d8)!,
                GuiLabel = "1d8 Sonic",
                GoldCost = MeleeGoldCost1D8,
                CraftingTier = CraftingTier.Greater
            }
        ],
        BaseDifficulty = 10
    };
}