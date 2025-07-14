using AmiaReforged.PwEngine.Systems.Crafting.Models;
using static NWN.Core.NWScript;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public class CastSpellProperties
{
    private const int CustomCastBattletide9 = 455;

    private const int CustomCastBlessWeapon17 = 517;

    private const int IpConstCastSpellHorizikaulsBoom2 = 459;
    private const int IpConstCastSpellIceDagger2 = 478;
    private const int IpConstCastSpellMagicWeapon = 479;
    private const int IpConstCastSpellShelgarns2 = 469;

    private const int IpConstCastSpellDeathArmor3 = 457;
    private const int IpConstCastSpellCombust3 = 456;
    private const int IpConstCastSpellCloudBewilderment3 = 486;
    private const int IpConstCastSpellUltravision3 = 309;
    private const int IpConstCastSpellFlameWeapon3 = 477;
    private const int IpConstCastSpellGedlees3 = 458;

    /* Useful spells:
     - Aura of Vitality (13)
- Belagarn's Iron Horn (7)
- Barkskin (12)
- War Cry (7)
- Bless Weapon (17)
- Cat's Grace (15)
- Haste (10)
- Improved Invisibility (7)
     */

    public static readonly CraftingCategory BeneficialSpells = new(categoryId: "beneficial_spells")
    {
        Label = "Beneficial Spells",
        Properties =
        [
            // Minor powers
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BURNING_HANDS_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Burning Hands (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CHARM_PERSON_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Charm Person (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_COLOR_SPRAY_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Color Spray (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_GREASE_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Grease (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellHorizikaulsBoom2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Horizikaul's Boom (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellIceDagger2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ice Dagger (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_IDENTIFY_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Identify (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_MAGE_ARMOR_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Mage Armor (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_MAGIC_MISSILE_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Magic Missile (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellMagicWeapon,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Magic Weapon (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_RAY_OF_ENFEEBLEMENT_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ray of Enfeeblement (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SCARE_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Scare (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SCARE_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Scare (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellShelgarns2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Shelgarn's Persistent Blade (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SLEEP_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Sleep (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SUMMON_CREATURE_I_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Summon Creature I (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BANE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Bane (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_DOOM_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Doom (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_INFLICT_LIGHT_WOUNDS_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Inflict Light Wounds (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_REMOVE_FEAR_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Remove Fear (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SHIELD_OF_FAITH_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Shield of Faith (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 5000
            },

            // Lesser powers
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BLINDNESS_DEAFNESS_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Blindness/Deafness (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellCloudBewilderment3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Cloud of Bewilderment (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellCombust3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Combust (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_DARKNESS_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Darkness (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellDeathArmor3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Death Armor (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellFlameWeapon3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Flame Weapon (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellGedlees3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Gedlee's Electric Loop (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_GHOSTLY_VISAGE_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ghostly Visage (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_GHOUL_TOUCH_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ghoul Touch (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_INVISIBILITY_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Invisibility (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_KNOCK_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Knock (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_LESSER_DISPEL_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Lesser Dispel (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_MELFS_ACID_ARROW_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Melf's Acid Arrow (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SEE_INVISIBILITY_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "See Invisibility (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SUMMON_CREATURE_II_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Summon Creature II (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellUltravision3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ultravision (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_WEB_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Web (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CURE_MODERATE_WOUNDS_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Cure Moderate Wounds (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SHIELD_OF_FAITH_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Shield of Faith (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_INFLICT_MODERATE_WOUNDS_7,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Inflict Moderate Wounds (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_LESSER_RESTORATION_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Lesser Restoration (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_REMOVE_PARALYSIS_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Remove Paralysis (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 10000
            },
        ],
        BaseDifficulty = 16
    };


    /* Fluff spells:
     - Aid (3)
- Bless (2)
- Cat's Grace (3)
- Expeditious Retreat (5)
- Light (1)
     */

    public static readonly CraftingCategory FluffSpells = new(categoryId: "fluff_spells")
    {
        Label = "Fluff Spells",
        Properties =
        [
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_AID_3,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Aid (3) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BLESS_2,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bless (2) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CATS_GRACE_3,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Cat's Grace (3) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            },
            // bull's strength
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BULLS_STRENGTH_3,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bull's Strength (3) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            },
            // endurance
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_ENDURANCE_3,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Endurance (3) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_EXPEDITIOUS_RETREAT_5,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Expeditious Retreat (5) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_LIGHT_1,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Light (1) / 2 Per Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Minor,
                GoldCost = 1000
            }
        ],
        BaseDifficulty = 16
    };
}