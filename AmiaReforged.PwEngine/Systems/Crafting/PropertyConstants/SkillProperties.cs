using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

/// <summary>
/// Constants for advantageous and RP flavor (Personal) skills. 
/// </summary>
public static class SkillProperties
{
    private const int SkillCost0 = 1000;
    private const int SkillCost1 = 5000;
    private const int SkillCost2 = 10000;

    /// <summary>
    /// Advantageous skills that can be added to items.
    /// </summary>
    public static readonly CraftingCategory Advantageous = new("advantageous")
    {
        Label = "Beneficial Skills",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_ANIMAL_EMPATHY, 10)!,
                GuiLabel = "+10 Animal Empathy",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CONCENTRATION, 10)!,
                GuiLabel = "+10 Concentration",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 10)!,
                GuiLabel = "+10 Craft Trap",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 10)!,
                GuiLabel = "+10 Disable Trap",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISCIPLINE, 10)!,
                GuiLabel = "+10 Discipline",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HEAL, 10)!,
                GuiLabel = "+10 Heal",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HIDE, 10)!,
                GuiLabel = "+10 Hide",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LISTEN, 10)!,
                GuiLabel = "+10 Listen",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_MOVE_SILENTLY, 10)!,
                GuiLabel = "+10 Move Silently",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 10)!,
                GuiLabel = "+10 Open Lock",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PARRY, 10)!,
                GuiLabel = "+10 Parry",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERFORM, 10)!,
                GuiLabel = "+10 Perform",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SEARCH, 10)!,
                GuiLabel = "+10 Search",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SET_TRAP, 10)!,
                GuiLabel = "+10 Set Trap",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPELLCRAFT, 10)!,
                GuiLabel = "+10 Spellcraft",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPOT, 10)!,
                GuiLabel = "+10 Spot",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TAUNT, 10)!,
                GuiLabel = "+10 Taunt",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_USE_MAGIC_DEVICE, 10)!,
                GuiLabel = "+10 Use Magic Device",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_ANIMAL_EMPATHY, 5)!,
                GuiLabel = "+5 Animal Empathy",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CONCENTRATION, 5)!,
                GuiLabel = "+5 Concentration",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 5)!,
                GuiLabel = "+5 Craft Trap",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 5)!,
                GuiLabel = "+5 Disable Trap",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISCIPLINE, 5)!,
                GuiLabel = "+5 Discipline",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HEAL, 5)!,
                GuiLabel = "+5 Heal",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HIDE, 5)!,
                GuiLabel = "+5 Hide",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LISTEN, 5)!,
                GuiLabel = "+5 Listen",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_MOVE_SILENTLY, 5)!,
                GuiLabel = "+5 Move Silently",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 5)!,
                GuiLabel = "+5 Open Lock",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PARRY, 5)!,
                GuiLabel = "+5 Parry",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERFORM, 5)!,
                GuiLabel = "+5 Perform",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SEARCH, 5)!,
                GuiLabel = "+5 Search",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SET_TRAP, 5)!,
                GuiLabel = "+5 Set Trap",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPELLCRAFT, 5)!,
                GuiLabel = "+5 Spellcraft",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPOT, 5)!,
                GuiLabel = "+5 Spot",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TAUNT, 5)!,
                GuiLabel = "+5 Taunt",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_USE_MAGIC_DEVICE, 5)!,
                GuiLabel = "+5 Use Magic Device",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_ANIMAL_EMPATHY, 2)!,
                GuiLabel = "+2 Animal Empathy",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 2)!,
                GuiLabel = "+2 Appraise",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CONCENTRATION, 2)!,
                GuiLabel = "+2 Concentration",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 2)!,
                GuiLabel = "+2 Craft Trap",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 2)!,
                GuiLabel = "+2 Disable Trap",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISCIPLINE, 2)!,
                GuiLabel = "+2 Discipline",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HEAL, 2)!,
                GuiLabel = "+2 Heal",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HIDE, 2)!,
                GuiLabel = "+2 Hide",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LISTEN, 2)!,
                GuiLabel = "+2 Listen",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_MOVE_SILENTLY, 2)!,
                GuiLabel = "+2 Move Silently",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 2)!,
                GuiLabel = "+2 Open Lock",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PARRY, 2)!,
                GuiLabel = "+2 Parry",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERFORM, 2)!,
                GuiLabel = "+2 Perform",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SEARCH, 2)!,
                GuiLabel = "+2 Search",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SET_TRAP, 2)!,
                GuiLabel = "+2 Set Trap",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPELLCRAFT, 2)!,
                GuiLabel = "+2 Spellcraft",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPOT, 2)!,
                GuiLabel = "+2 Spot",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TAUNT, 2)!,
                GuiLabel = "+2 Taunt",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_USE_MAGIC_DEVICE, 2)!,
                GuiLabel = "+2 Use Magic Device",
                GoldCost = SkillCost0,
                CraftingTier = CraftingTier.Lesser
            }
        },
        BaseDifficulty = 8,
    };

    /// <summary>
    /// Personal skills that can be added to items.
    /// </summary>
    public static readonly CraftingCategory Personal = new("personal")
    {
        Label = "Roleplay/Personal Skills",
        Properties = new[]
        {
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 5)!,
                GuiLabel = "+5 Appraise",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_BLUFF, 5)!,
                GuiLabel = "+5 Bluff",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_ARMOR, 5)!,
                GuiLabel = "+5 Craft Armor",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_WEAPON, 5)!,
                GuiLabel = "+5 Craft Weapon",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_INTIMIDATE, 5)!,
                GuiLabel = "+5 Intimidate",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LORE, 5)!,
                GuiLabel = "+5 Lore",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERSUADE, 5)!,
                GuiLabel = "+5 Persuade",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PICK_POCKET, 5)!,
                GuiLabel = "+5 Pick Pocket",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_RIDE, 5)!,
                GuiLabel = "+5 Ride",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 0,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TUMBLE, 5)!,
                GuiLabel = "+5 Tumble",
                GoldCost = SkillCost1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 10)!,
                GuiLabel = "+10 Appraise",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_BLUFF, 10)!,
                GuiLabel = "+10 Bluff",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_ARMOR, 10)!,
                GuiLabel = "+10 Craft Armor",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_WEAPON, 10)!,
                GuiLabel = "+10 Craft Weapon",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_INTIMIDATE, 10)!,
                GuiLabel = "+10 Intimidate",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LORE, 10)!,
                GuiLabel = "+10 Lore",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERSUADE, 10)!,
                GuiLabel = "+10 Persuade",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PICK_POCKET, 10)!,
                GuiLabel = "+10 Pick Pocket",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_RIDE, 10)!,
                GuiLabel = "+10 Ride",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TUMBLE, 10)!,
                GuiLabel = "+10 Tumble",
                GoldCost = SkillCost2,
                CraftingTier = CraftingTier.Greater
            }
        },
        BaseDifficulty = 8,
    };
}