using AmiaReforged.PwEngine.Features.Crafting.Models;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.ItemProperties;

public static class BonusSpellSlotProperties
{
    private const int MythalCostBslot1 = 10000;
    private const int MythalCostBslot2 = 20000;
    private const int MythalCostBslot3 = 30000;
    private const int MythalCostBslot4 = 40000;
    private const int MythalCostBslot5 = 50000;
    private const int MythalCostBslot6 = 60000;
    private const int MythalCostBslot7 = 70000;
    private const int MythalCostBslot8 = 80000;
    private const int MythalCostBslot9 = 90000;

    private static readonly int[] GoldCosts =
    [
        MythalCostBslot1, MythalCostBslot2, MythalCostBslot3, MythalCostBslot4, MythalCostBslot5,
        MythalCostBslot6, MythalCostBslot7, MythalCostBslot8, MythalCostBslot9, MythalCostBslot9
    ];

    /// <summary>
    /// Factory method to create bonus spell slot categories with configurable power cost.
    /// </summary>
    /// <param name="categoryId">Unique category identifier</param>
    /// <param name="label">Display label for the category</param>
    /// <param name="classType">The NWN class type constant (e.g., NWScript.IP_CONST_CLASS_WIZARD)</param>
    /// <param name="className">Display name for the class (e.g., "Wizard")</param>
    /// <param name="exclusiveClass">The ClassType enum for class exclusivity</param>
    /// <param name="powerCost">Power cost per spell slot (always 1 point)</param>
    /// <param name="startLevel">Starting spell level (0 for full casters, 1 for partial casters)</param>
    /// <param name="maxLevel">Maximum spell level available</param>
    /// <returns>A configured CraftingCategory for bonus spell slots</returns>
    private static CraftingCategory CreateBonusSpellCategory(
        string categoryId,
        string label,
        int classType,
        string className,
        ClassType exclusiveClass,
        int powerCost,
        int startLevel,
        int maxLevel)
    {
        var properties = new List<CraftingProperty>();

        for (int level = startLevel; level <= maxLevel; level++)
        {
            properties.Add(new CraftingProperty
            {
                PowerCost = powerCost,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(classType, level)!,
                GuiLabel = $"{className} Slot: Level {level}",
                GoldCost = GoldCosts[level],
                CraftingTier = CraftingTier.Perfect
            });
        }

        return new CraftingCategory(categoryId: categoryId)
        {
            Label = label,
            Properties = properties,
            ExclusiveToClass = true,
            ExclusiveClass = exclusiveClass,
            BaseDifficulty = 25
        };
    }

    // ========== Caster Weapon Categories (PowerCost = 1) ==========
    public static readonly CraftingCategory AssassinBonusSpells =
        CreateBonusSpellCategory("assassin_bonus_spells", "Assassin Bonus Spells",
            NWScript.CLASS_TYPE_ASSASSIN, "Assassin", ClassType.Assassin, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory BardBonusSpells =
        CreateBonusSpellCategory("bard_bonus_spells", "Bard Bonus Spells",
            NWScript.IP_CONST_CLASS_BARD, "Bard", ClassType.Bard, powerCost: 1, startLevel: 0, maxLevel: 6);

    public static readonly CraftingCategory BlackguardBonusSpells =
        CreateBonusSpellCategory("blackguard_bonus_spells", "Blackguard Bonus Spells",
            NWScript.CLASS_TYPE_BLACKGUARD, "Blackguard", ClassType.Blackguard, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory ClericBonusSpells =
        CreateBonusSpellCategory("cleric_bonus_spells", "Cleric Bonus Spells",
            NWScript.IP_CONST_CLASS_CLERIC, "Cleric", ClassType.Cleric, powerCost: 1, startLevel: 0, maxLevel: 9);

    public static readonly CraftingCategory DruidBonusSpells =
        CreateBonusSpellCategory("druid_bonus_spells", "Druid Bonus Spells",
            NWScript.IP_CONST_CLASS_DRUID, "Druid", ClassType.Druid, powerCost: 1, startLevel: 0, maxLevel: 9);

    public static readonly CraftingCategory PaladinBonusSpells =
        CreateBonusSpellCategory("paladin_bonus_spells", "Paladin Bonus Spells",
            NWScript.IP_CONST_CLASS_PALADIN, "Paladin", ClassType.Paladin, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory RangerBonusSpells =
        CreateBonusSpellCategory("ranger_bonus_spells", "Ranger Bonus Spells",
            NWScript.IP_CONST_CLASS_RANGER, "Ranger", ClassType.Ranger, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory SorcererBonusSpells =
        CreateBonusSpellCategory("sorcerer_bonus_spells", "Sorcerer Bonus Spells",
            NWScript.IP_CONST_CLASS_SORCERER, "Sorcerer", ClassType.Sorcerer, powerCost: 1, startLevel: 0, maxLevel: 9);

    public static readonly CraftingCategory WizardBonusSpells =
        CreateBonusSpellCategory("wizard_bonus_spells", "Wizard Bonus Spells",
            NWScript.IP_CONST_CLASS_WIZARD, "Wizard", ClassType.Wizard, powerCost: 1, startLevel: 0, maxLevel: 9);

    // ========== Equipped Item Categories (PowerCost = 1) ==========
    public static readonly CraftingCategory AssassinBonusSpellsCostly =
        CreateBonusSpellCategory("assassin_bonus_spells_costly", "Assassin Bonus Spells",
            NWScript.CLASS_TYPE_ASSASSIN, "Assassin", ClassType.Assassin, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory BardBonusSpellsCostly =
        CreateBonusSpellCategory("bard_bonus_spells_costly", "Bard Bonus Spells",
            NWScript.IP_CONST_CLASS_BARD, "Bard", ClassType.Bard, powerCost: 1, startLevel: 0, maxLevel: 6);

    public static readonly CraftingCategory BlackguardBonusSpellsCostly =
        CreateBonusSpellCategory("blackguard_bonus_spells_costly", "Blackguard Bonus Spells",
            NWScript.CLASS_TYPE_BLACKGUARD, "Blackguard", ClassType.Blackguard, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory ClericBonusSpellsCostly =
        CreateBonusSpellCategory("cleric_bonus_spells_costly", "Cleric Bonus Spells",
            NWScript.IP_CONST_CLASS_CLERIC, "Cleric", ClassType.Cleric, powerCost: 1, startLevel: 0, maxLevel: 9);

    public static readonly CraftingCategory DruidBonusSpellsCostly =
        CreateBonusSpellCategory("druid_bonus_spells_costly", "Druid Bonus Spells",
            NWScript.IP_CONST_CLASS_DRUID, "Druid", ClassType.Druid, powerCost: 1, startLevel: 0, maxLevel: 9);

    public static readonly CraftingCategory PaladinBonusSpellsCostly =
        CreateBonusSpellCategory("paladin_bonus_spells_costly", "Paladin Bonus Spells",
            NWScript.IP_CONST_CLASS_PALADIN, "Paladin", ClassType.Paladin, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory RangerBonusSpellsCostly =
        CreateBonusSpellCategory("ranger_bonus_spells_costly", "Ranger Bonus Spells",
            NWScript.IP_CONST_CLASS_RANGER, "Ranger", ClassType.Ranger, powerCost: 1, startLevel: 1, maxLevel: 4);

    public static readonly CraftingCategory SorcererBonusSpellsCostly =
        CreateBonusSpellCategory("sorcerer_bonus_spells_costly", "Sorcerer Bonus Spells",
            NWScript.IP_CONST_CLASS_SORCERER, "Sorcerer", ClassType.Sorcerer, powerCost: 1, startLevel: 0, maxLevel: 9);

    public static readonly CraftingCategory WizardBonusSpellsCostly =
        CreateBonusSpellCategory("wizard_bonus_spells_costly", "Wizard Bonus Spells",
            NWScript.IP_CONST_CLASS_WIZARD, "Wizard", ClassType.Wizard, powerCost: 1, startLevel: 0, maxLevel: 9);
}
