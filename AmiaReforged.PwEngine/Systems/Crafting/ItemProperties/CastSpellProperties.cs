using AmiaReforged.PwEngine.Systems.Crafting.Models;
using static NWN.Core.NWScript;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public class CastSpellProperties
{
    private const int CustomCastBattletide9 = 455;

    private const int CustomCastBlessWeapon17 = 517;
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
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BALAGARNSIRONHORN_7,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Balagarn's Iron Horn (7) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BARKSKIN_12,
                    IP_CONST_CASTSPELL_NUMUSES_3_USES_PER_DAY)!,
                GuiLabel = "Barkskin (12) / 3 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_WAR_CRY_7,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "War Cry (7) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 8000
            },
            new CraftingProperty
            {
                ItemProperty =
                    ItemPropertyCastSpell(CustomCastBlessWeapon17, IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bless Weapon (17) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 15000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_CATS_GRACE_15,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Cat's Grace (15) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 5000
            },
            // bull's strength
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_BULLS_STRENGTH_15,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Bull's Strength (15) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 5000
            },
            // endurance
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_ENDURANCE_15,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Endurance (15) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 5000
            },
            new CraftingProperty
            {
                ItemProperty =
                    ItemPropertyCastSpell(IP_CONST_CASTSPELL_HASTE_10, IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Haste (10) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(IP_CONST_CASTSPELL_IMPROVED_INVISIBILITY_7,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Improved Invisibility (7) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 10000
            },
            new CraftingProperty
            {
                ItemProperty = ItemPropertyCastSpell(CustomCastBattletide9,
                    IP_CONST_CASTSPELL_NUMUSES_2_USES_PER_DAY)!,
                GuiLabel = "Battletide (9) / 2 Per Day",
                PowerCost = 2,
                CraftingTier = CraftingTier.Intermediate,
                GoldCost = 10000
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
}