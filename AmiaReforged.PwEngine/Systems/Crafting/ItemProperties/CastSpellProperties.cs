using AmiaReforged.PwEngine.Systems.Crafting.Models;
using static NWN.Core.NWScript;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public class CastSpellProperties
{
    private const int CustomCastBattletide9 = 455;

    private const int CustomCastBlessWeapon17 = 517;

    private const int IpConstCastspellHorizikaulsBoom2 = 459;
    private const int IpConstCastspellIceDagger2 = 478;
    private const int IpConstCastspellMagicWeapon = 479;
    private const int IpConstCastspellShelgarns2 = 469;
    
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
            // Lesser powers
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BURNING_HANDS_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Burning Hands (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CHARM_PERSON_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Charm Person (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_COLOR_SPRAY_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Color Spray (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_GREASE_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Grease (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastspellHorizikaulsBoom2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Horizikaul's Boom (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastspellIceDagger2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ice Dagger (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_IDENTIFY_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Identify (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_MAGE_ARMOR_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Mage Armor (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_MAGIC_MISSILE_3,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Magic Missile (3) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastspellMagicWeapon,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Magic Weapon (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_RAY_OF_ENFEEBLEMENT_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Ray of Enfeeblement (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SCARE_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Scare (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SCARE_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Scare (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IpConstCastspellShelgarns2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Shelgarn's Persistent Blade (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SLEEP_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Sleep (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SUMMON_CREATURE_I_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Summon Creature I (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BANE_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Bane (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_DOOM_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Doom (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_INFLICT_LIGHT_WOUNDS_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Inflict Light Wounds (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_REMOVE_FEAR_2,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Remove Fear (2) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_SHIELD_OF_FAITH_5,
                    IP_CONST_CASTSPELL_NUMUSES_1_USE_PER_DAY)!,
                GuiLabel = "Shield of Faith (5) 1/Day",
                PowerCost = 0,
                CraftingTier = CraftingTier.Lesser,
                GoldCost = 5000
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