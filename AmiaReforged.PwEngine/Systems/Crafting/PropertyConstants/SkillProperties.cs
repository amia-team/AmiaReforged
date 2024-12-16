using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

/// <summary>
/// Constants for advantageous and RP flavor (Personal) skills. 
/// </summary>
public static class SkillProperties
{
    /// <summary>
    /// Advantageous skills that can be added to items.
    /// </summary>
    public static readonly CraftingPropertyCategory Advantageous = new()
    {
        Label = "Beneficial Skills",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_ANIMAL_EMPATHY, 10)!,
                GuiLabel = "+10 Animal Empathy",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 10)!,
                GuiLabel = "+10 Appraise",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CONCENTRATION, 10)!,
                GuiLabel = "+10 Concentration",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 10)!,
                GuiLabel = "+10 Craft Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 10)!,
                GuiLabel = "+10 Disable Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISCIPLINE, 10)!,
                GuiLabel = "+10 Discipline",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HEAL, 10)!,
                GuiLabel = "+10 Heal",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HIDE, 10)!,
                GuiLabel = "+10 Hide",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LISTEN, 10)!,
                GuiLabel = "+10 Listen",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_MOVE_SILENTLY, 10)!,
                GuiLabel = "+10 Move Silently",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 10)!,
                GuiLabel = "+10 Open Lock",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PARRY, 10)!,
                GuiLabel = "+10 Parry",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERFORM, 10)!,
                GuiLabel = "+10 Perform",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SEARCH, 10)!,
                GuiLabel = "+10 Search",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SET_TRAP, 10)!,
                GuiLabel = "+10 Set Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPELLCRAFT, 10)!,
                GuiLabel = "+10 Spellcraft",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPOT, 10)!,
                GuiLabel = "+10 Spot",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TAUNT, 10)!,
                GuiLabel = "+10 Taunt",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 4,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_USE_MAGIC_DEVICE, 10)!,
                GuiLabel = "+10 Use Magic Device",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_ANIMAL_EMPATHY, 5)!,
                GuiLabel = "+5 Animal Empathy",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 5)!,
                GuiLabel = "+5 Appraise",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CONCENTRATION, 5)!,
                GuiLabel = "+5 Concentration",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 5)!,
                GuiLabel = "+5 Craft Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 5)!,
                GuiLabel = "+5 Disable Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISCIPLINE, 5)!,
                GuiLabel = "+5 Discipline",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HEAL, 5)!,
                GuiLabel = "+5 Heal",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HIDE, 5)!,
                GuiLabel = "+5 Hide",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LISTEN, 5)!,
                GuiLabel = "+5 Listen",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_MOVE_SILENTLY, 5)!,
                GuiLabel = "+5 Move Silently",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 5)!,
                GuiLabel = "+5 Open Lock",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PARRY, 5)!,
                GuiLabel = "+5 Parry",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERFORM, 5)!,
                GuiLabel = "+5 Perform",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SEARCH, 5)!,
                GuiLabel = "+5 Search",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SET_TRAP, 5)!,
                GuiLabel = "+5 Set Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPELLCRAFT, 5)!,
                GuiLabel = "+5 Spellcraft",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPOT, 5)!,
                GuiLabel = "+5 Spot",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TAUNT, 5)!,
                GuiLabel = "+5 Taunt",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_USE_MAGIC_DEVICE, 5)!,
                GuiLabel = "+5 Use Magic Device",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_ANIMAL_EMPATHY, 2)!,
                GuiLabel = "+2 Animal Empathy",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 2)!,
                GuiLabel = "+2 Appraise",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CONCENTRATION, 2)!,
                GuiLabel = "+2 Concentration",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 2)!,
                GuiLabel = "+2 Craft Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 2)!,
                GuiLabel = "+2 Disable Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISCIPLINE, 2)!,
                GuiLabel = "+2 Discipline",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HEAL, 2)!,
                GuiLabel = "+2 Heal",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_HIDE, 2)!,
                GuiLabel = "+2 Hide",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LISTEN, 2)!,
                GuiLabel = "+2 Listen",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_MOVE_SILENTLY, 2)!,
                GuiLabel = "+2 Move Silently",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 2)!,
                GuiLabel = "+2 Open Lock",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PARRY, 2)!,
                GuiLabel = "+2 Parry",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERFORM, 2)!,
                GuiLabel = "+2 Perform",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SEARCH, 2)!,
                GuiLabel = "+2 Search",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SET_TRAP, 2)!,
                GuiLabel = "+2 Set Trap",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPELLCRAFT, 2)!,
                GuiLabel = "+2 Spellcraft",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_SPOT, 2)!,
                GuiLabel = "+2 Spot",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TAUNT, 2)!,
                GuiLabel = "+2 Taunt",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_USE_MAGIC_DEVICE, 2)!,
                GuiLabel = "+2 Use Magic Device",
                CraftingTier = CraftingTier.Greater
            }
        }
    };

    /// <summary>
    /// Personal skills that can be added to items.
    /// </summary>
    public static readonly CraftingPropertyCategory Personal = new()
    {
        Label = "Roleplay/Personal Skills",
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 5)!,
                GuiLabel = "+5 Appraise",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_BLUFF, 5)!,
                GuiLabel = "+5 Bluff",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_ARMOR, 5)!,
                GuiLabel = "+5 Craft Armor",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_WEAPON, 5)!,
                GuiLabel = "+5 Craft Weapon",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_INTIMIDATE, 5)!,
                GuiLabel = "+5 Intimidate",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LORE, 5)!,
                GuiLabel = "+5 Lore",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERSUADE, 5)!,
                GuiLabel = "+5 Persuade",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PICK_POCKET, 5)!,
                GuiLabel = "+5 Pick Pocket",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_RIDE, 5)!,
                GuiLabel = "+5 Ride",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 0,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TUMBLE, 5)!,
                GuiLabel = "+5 Tumble",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_APPRAISE, 10)!,
                GuiLabel = "+10 Appraise",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_BLUFF, 10)!,
                GuiLabel = "+10 Bluff",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_ARMOR, 10)!,
                GuiLabel = "+10 Craft Armor",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_WEAPON, 10)!,
                GuiLabel = "+10 Craft Weapon",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_INTIMIDATE, 10)!,
                GuiLabel = "+10 Intimidate",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_LORE, 10)!,
                GuiLabel = "+10 Lore",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PERSUADE, 10)!,
                GuiLabel = "+10 Persuade",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PICK_POCKET, 10)!,
                GuiLabel = "+10 Pick Pocket",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_RIDE, 10)!,
                GuiLabel = "+10 Ride",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_TUMBLE, 10)!,
                GuiLabel = "+10 Tumble",
                CraftingTier = CraftingTier.Greater
            }
        }
    };
}