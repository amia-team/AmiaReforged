using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

/// <summary>
/// Constants for beneficial and RP flavor skills. 
/// </summary>
public static class SkillProperties
{
    /// <summary>
    /// Beneficial skills that can be added to items.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> Beneficial = new[]
    {
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
            Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_DISABLE_TRAP, 5)!,
            GuiLabel = "+5 Diplomacy",
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
            Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_PICK_POCKET, 5)!,
            GuiLabel = "+5 Pick Pocket",
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
        }
    };

    /// <summary>
    /// Flavor skills that can be added to items.
    /// </summary>
    public static readonly IReadOnlyList<CraftingProperty> Roleplay = new[]
    {
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
            Cost = 0,
            Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_OPEN_LOCK, 5)!,
            GuiLabel = "+5 Open Lock",
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
            Property = NWScript.ItemPropertySkillBonus(NWScript.SKILL_CRAFT_TRAP, 5)!,
            GuiLabel = "+5 Craft Trap",
            CraftingTier = CraftingTier.Greater
        }
    };
}