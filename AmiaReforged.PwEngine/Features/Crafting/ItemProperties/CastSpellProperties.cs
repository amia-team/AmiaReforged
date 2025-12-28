using AmiaReforged.PwEngine.Features.Crafting.Models;
using static NWN.Core.NWScript;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

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

    private const int IpConstCastSpellMestilsHangoverBreath5 = 461;
    private const int IpConstCastSpellScintSphere5 = 464;
    private const int IpConstCastSpellEdgyFlameWeapon5 = 483;
    private const int IpConstCastSpellGlyphWarding5 = 484;
    private const int IpConstCastSpellMagicVestments5 = 481;

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

            // Intermediate powers
            new CraftingProperty
            {
                GuiLabel = "Clairaudience/Clairvoyance (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CLAIRAUDIENCE_CLAIRVOYANCE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Clarity (3) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CLARITY_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Find Traps (3) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_FIND_TRAPS_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Fireball (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_FIREBALL_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Flame Arrow (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_FLAME_ARROW_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Haste (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_HASTE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Hold Person (3) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_HOLD_PERSON_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Invisibility Sphere (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_INVISIBILITY_SPHERE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Lightning Bolt (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_LIGHTNING_BOLT_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Magic Circle Against Alignment (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_MAGIC_CIRCLE_AGAINST_ALIGNMENT_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Mestil's Acid Breath (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellMestilsHangoverBreath5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Scintillating Sphere (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellScintSphere5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Slow (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SLOW_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Stinking Cloud (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_STINKING_CLOUD_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Summon Creature III (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SUMMON_CREATURE_III_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Vampiric Touch (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_VAMPIRIC_TOUCH_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Contagion (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CONTAGION_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Cure Serious Wounds (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CURE_SERIOUS_WOUNDS_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Darkfire (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellEdgyFlameWeapon5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Glyph of Warding (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellGlyphWarding5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Magic Vestment (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IpConstCastSpellMagicVestments5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Negative Energy Protection (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_NEGATIVE_ENERGY_PROTECTION_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Remove Blindness/Deafness (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_REMOVE_BLINDNESS_DEAFNESS_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Remove Curse (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_REMOVE_CURSE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Remove Disease (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_REMOVE_DISEASE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                GuiLabel = "Searing Light (5) 1/Day",
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SEARING_LIGHT_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                PowerCost = 0,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            }
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
    public static readonly CraftingCategory DMSpellCasting = new(categoryId: "dm_spell_casting")
    {
        Label = "DM Spell Casting (All Spells)",
        Properties =
        [
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(0,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Acid Fog (11) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(1,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Aid (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(4,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Animate Dead (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(6,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Barkskin (6) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(8,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bestow Curse (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(10,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Blade Barrier (15) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(11,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Bless (2) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(14,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Blindness/Deafness (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(17,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bull's Strength (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(19,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Burning Hands (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(21,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Call Lightning (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(27,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Cat's Grace (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(30,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Chain Lightning (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(32,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Charm Monster (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(34,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Charm Person (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(36,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Charm Person or Animal (10) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(39,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Circle of Death (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(42,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Circle of Doom (20) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(45,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Clairaudience/Clairvoyance (15) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(46,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Clarity (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(48,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Cloudkill (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(49,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Color Spray (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(51,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Cone of Cold (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(53,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Confusion (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(54,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Contagion (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(56,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Control Undead (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(59,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Create Greater Undead (18) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(62,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Create Undead (16) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(65,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Cure Critical Wounds (15) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(67,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Cure Light Wounds (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(67,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Cure Light Wounds (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(68,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Cure Minor Wounds (1) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(68,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Cure Minor Wounds (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(69,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Cure Moderate Wounds (10) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(74,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Cure Serious Wounds (15) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(75,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Darkness (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(76,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Daze (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(77,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Death Ward (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(80,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Delayed Blast Fireball (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(83,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Dismissal (18) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(85,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dispel Magic (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(86,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Divine_Power (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(87,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Dominate Animal (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(88,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Dominate Monster (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(89,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Dominate Person (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(91,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Doom (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(93,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Elemental Shield (12) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(97,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Endurance (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(98,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Endure Elements (2) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(100,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Enervation (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(101,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Entangle (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(102,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Entangle (5) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(103,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Fear (5) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(104,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Feeblemind (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(105,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Finger of Death (13) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(107,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Fire Storm (18) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(109,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Fireball (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(112,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Flame Arrow (18) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(114,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Flame Lash (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(117,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Flame Strike (18) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(118,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Freedom of Movement (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(119,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Gate (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(120,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ghoul Touch (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(122,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Grease (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(126,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Greater Planar Binding (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(129,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Greater Spell Breach (11) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(130,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Greater Spell Mantle (17) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(134,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Hammer of the Gods (12) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(136,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Harm (11) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(138,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Haste (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(139,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Heal (11) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(141,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Healing Circle (16) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(142,
                    IP_CONST_CASTSPELL_NUMUSES_4_USES_PER_DAY)!,
                GuiLabel = "Hold Animal (3) 4/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(143,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Hold Monster (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(144,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Hold Person (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(147,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Identify (3) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(148,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Implosion (17) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(149,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Improved Invisibility (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(150,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "IncendiaryCloud (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(151,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Invisibility (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(152,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Invisibility Purge (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(153,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Invisibility Sphere (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(154,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Knock (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(156,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lesser Dispel (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(157,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lesser Mind Blank (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(158,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lesser Planar Binding (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(159,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lesser Restoration (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(160,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lesser Spell Breach (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(161,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lesser Spell Mantle (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(162,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Light (1) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(163,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Light (5) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(165,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Lightning Bolt (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(167,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Mage Armor (2) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(174,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Magic Missile (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(179,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Mass Blindness/Deafness (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(180,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Mass Charm (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(182,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Mass Haste (11) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(186,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Melf's Acid Arrow (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(187,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Meteor Swarm (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(189,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Mind Fog (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(194,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ghostly Visage (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(196,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ethereal Visage (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(199,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Mordenkainen's Sword (18) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(200,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Nature's Balance (15) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(203,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Negative Energy Protection (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(204,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Neutralize Poison (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(205,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Phantasmal Killer (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(206,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Planar Binding (11) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(207,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Poison (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(208,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Polymorph Self (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(209,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Power Word: Kill (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(210,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Power Word: Stun (13) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(211,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Prayer (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(213,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Prismatic Spray (13) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(217,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Protection from Elements (10) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(226,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Raise Dead (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(227,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ray of Enfeeblement (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(228,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Ray of Frost (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(229,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Remove Blindness/Deafness (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(230,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Remove Curse (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(231,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Remove Disease (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(232,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Remove Fear (2) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(233,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Remove Paralysis (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(235,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Resist Elements (10) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(237,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Resistance (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(238,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Restoration (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(240,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Sanctuary (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(241,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Scare (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(242,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Searing Light (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(243,
                    IP_CONST_CASTSPELL_NUMUSES_4_USES_PER_DAY)!,
                GuiLabel = "See Invisibility (3) 4/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(244,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Shades (11) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(245,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Shadow Conjuration (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(247,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Shapechange (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(249,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Silence (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(250,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Slay Living (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(252,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Sleep (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(253,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Slow (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(254,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Sound Burst (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(257,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Spell Mantle (13) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(259,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Stinking Cloud (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(260,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Stoneskin (7) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(263,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Summon Creature I (5) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(264,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Creature II (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(265,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Summon Creature III (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(266,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Summon Creature IV (7) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(267,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Creature IX (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(268,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Summon Creature V (9) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(269,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Creature VI (11) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(270,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Creature VII (13) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(271,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Creature VIII (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(272,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Sunbeam (13) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(273,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Tenser's Transformation (11) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(275,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "True Seeing (9) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(277,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Vampiric Touch (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(278,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Virtue (1) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(278,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Virtue (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(279,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Wail of the Banshee (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(280,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Wall of Fire (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(281,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Web (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(282,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Weird (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(285,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Protection from Alignment (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(286,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Magic Circle Against Alignment (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(290,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Eagle's Splendor (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(293,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Owl's Wisdom (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(296,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Fox's Cunning (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(303,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Awaken (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(304,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Creeping Doom (13) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(306,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Ultravision (6) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(309,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Horrid Wilting (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(310,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ice Storm (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(315,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Negative Energy Burst (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(320,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Negative Energy Ray (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(322,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "War Cry (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(325,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Evard's Black Tentacles (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(326,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Legend Lore (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(327,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Find Traps (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(328,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Rogue's Cunning (3) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(345,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Divine Favor (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(346,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "True Strike (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(347,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Flare (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(348,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Shield (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(349,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Entropic Shield (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(350,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Continual Flame (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(351,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "One With the Land (7) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(352,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Camouflage (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(354,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Bombardment (20) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(355,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Acid Splash (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(356,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Quillfire (8) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(357,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Earthquake (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(358,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Sunburst (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(361,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Banishment (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(362,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Inflict Minor Wounds (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(363,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Inflict Light Wounds (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(364,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Inflict Moderate Wounds (7) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(365,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Inflict Serious Wounds (9) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(366,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Inflict Critical Wounds (12) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(367,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Balagarn's Iron Horn (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(368,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Drown (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(370,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Electric Jolt (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(371,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Firebrand (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(372,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Wounding Whispers (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(373,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Amplify (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(374,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Etherealness (18) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(376,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dirge (15) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(377,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Inferno (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(378,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Isaac's Lesser Missile Storm (13) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(380,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bane (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(381,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Shield of Faith (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(382,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Planar Ally (15) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(383,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Magic Fang (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(384,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Greater Magic Fang (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(385,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Spike Growth (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(387,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Expeditious Retreat (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(387,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Expeditious Retreat (5) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(388,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Tasha's Hideous Laughter (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(389,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Displacement (9) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(398,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Flesh to Stone (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(399,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Stone to Flesh (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(400,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Acid (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(401,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Cold (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(402,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Fear (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(403,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Fire (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(404,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Gas (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(405,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Lightning (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(406,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Paralyze (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(407,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Sleep (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(408,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Slow (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(409,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Weaken (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(410,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Gust of Wind (10) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(418,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Elemental: Air (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(419,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Elemental: Water (6) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(420,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Elemental: Earth (7) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(421,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Summon Elemental: Fire (8) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(439,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Manticore Spikes (1) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(439,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Manticore Spikes (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(450,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Crumble (11) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(451,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Infestation of Maggots (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(452,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Healing Sting (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(453,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Great Thunderclap (13) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(455,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Battletide (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(456,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Combust (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(457,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Death Armor (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(458,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Gedlee's Electric Loop (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(459,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Horizikaul's Boom (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(460,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Ironguts (2) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(461,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Mestils Acid Breath (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(462,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Mestils Acid Sheath (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(463,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Monstrous Regeneration (9) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(465,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Stone Bones (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(466,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Undeath to Death (11) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(467,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Vine Mine (9) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(468,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Black Blade of Disaster (17) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(469,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Shelgarn's Persistent Blade (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(472,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Bless Weapon (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(478,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ice Dagger (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(479,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Magic Weapon (2) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(481,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Magic Vestment (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(482,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Stonehold (11) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(483,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Darkfire (5) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(484,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Glyph of Warding (5) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(486,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Cloud of Bewilderment (3) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(511,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Greater Magic Weapon (10) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(515,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Flame Weapon (17) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(516,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Bless Weapon (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(538,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Ball Lightning (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(539,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Scintillating Sphere (20) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(579,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Hurl Rock (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(579,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Hurl Rock (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(580,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Gaze: Dominate (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(581,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Pulse: Fire (1) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(582,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Pulse: Lightning (1) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(583,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Pulse: Cold (1) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(584,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Pulse: Negative (1) 2/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(585,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Pulse: Holy (1) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(586,
                    IP_CONST_CASTSPELL_NUMUSES_5_USES_PER_DAY)!,
                GuiLabel = "Pulse: Poison (1) 5/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(587,
                    IP_CONST_CASTSPELL_NUMUSES_5_USES_PER_DAY)!,
                GuiLabel = "Pulse: Disease (1) 5/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(588,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Fear (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(589,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Doom (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(590,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Acid (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(590,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Bolt: Acid (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(591,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Cold (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(591,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Bolt: Cold (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(592,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Fire (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(592,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Bolt: Fire (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(593,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Lightning (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(593,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Bolt: Lightning (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(594,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Shards (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(594,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Bolt: Shards (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(595,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Sonic (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(596,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Confuse (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(597,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Daze (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(598,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Paralysis (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(599,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Howl: Stun (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(600,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Gaze: Confusion (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(601,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Gaze: Daze (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(602,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Gaze: Doom (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(603,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Gaze: Fear (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(604,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Disease (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(605,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Bolt: Poison (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(606,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Ray: Frost (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(606,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Ray: Frost (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(607,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Ray: Flame (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(607,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Ray: Flame (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(608,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Ray: Inflict Wounds (1) 3/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(608,
                    IP_CONST_CASTSPELL_NUMUSES_UNLIMITED_USE)!,
                GuiLabel = "Ray: Inflict Wounds (1) Unlimited",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(609,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Dragon Breath: Negative (10) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Wondrous,
                GoldCost = 1000
            }
        ],
        BaseDifficulty = 16
    };

}