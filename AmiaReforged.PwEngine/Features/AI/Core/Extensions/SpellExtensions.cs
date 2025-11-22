using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Extensions;

/// <summary>
/// Extension methods for Spell providing categorization and filtering.
/// Uses 2DA data from spells.2da and categories.2da for spell metadata.
/// Category IDs reference categories.2da (1-23), not simple 0-3 values.
/// </summary>
public static class SpellExtensions
{
    private static TwoDimArray? _spellTable;
    private static TwoDimArray SpellTable => _spellTable ??= NwGameTables.GetTable("spells")!;

    // Category IDs from categories.2da
    private const int CategoryHarmfulAoeDiscriminant = 1;
    private const int CategoryHarmfulRanged = 2;
    private const int CategoryHarmfulTouch = 3;
    private const int CategoryBeneficialHealingAoe = 4;
    private const int CategoryBeneficialHealingTouch = 5;
    private const int CategoryBeneficialConditionalAoe = 6;
    private const int CategoryBeneficialConditionalSingle = 7;
    private const int CategoryBeneficialEnhancementAoe = 8;
    private const int CategoryBeneficialEnhancementSingle = 9;
    private const int CategoryBeneficialEnhancementSelf = 10;
    private const int CategoryHarmfulAoeIndiscriminant = 11;
    private const int CategoryBeneficialProtectionSelf = 12;
    private const int CategoryBeneficialProtectionSingle = 13;
    private const int CategoryBeneficialProtectionAoe = 14;
    private const int CategoryBeneficialSummon = 15;
    private const int CategoryPersistentAoe = 16;
    private const int CategoryBeneficialHealingPotion = 17;
    private const int CategoryBeneficialConditionalPotion = 18;
    private const int CategoryDragonsBreath = 19;
    private const int CategoryBeneficialProtectionPotion = 20;
    private const int CategoryBeneficialEnhancementPotion = 21;
    private const int CategoryHarmfulMelee = 22;
    private const int CategoryDispel = 23;

    /// <summary>
    /// Gets the category ID from spells.2da Category column.
    /// This ID references categories.2da.
    /// </summary>
    public static int GetCategoryId(this Spell spell)
    {
        string? categoryStr = SpellTable.GetString((int)spell, "Category");
        if (!string.IsNullOrEmpty(categoryStr) && int.TryParse(categoryStr, out int categoryId))
        {
            return categoryId;
        }
        return 0;
    }

    #region Simplified AI Categorization

    /// <summary>
    /// Checks if this is an offensive/harmful spell (for AI attack behavior).
    /// Includes: Harmful AoE, Harmful Ranged, Harmful Touch, Harmful Melee, Dragon's Breath.
    /// </summary>
    public static bool IsAttackSpell(this Spell spell)
    {
        int categoryId = spell.GetCategoryId();
        return categoryId switch
        {
            CategoryHarmfulAoeDiscriminant => true,
            CategoryHarmfulRanged => true,
            CategoryHarmfulTouch => true,
            CategoryHarmfulAoeIndiscriminant => true,
            CategoryHarmfulMelee => true,
            CategoryDragonsBreath => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if this is a healing spell (for AI healing behavior).
    /// Includes: All healing categories (AoE, Touch, Potion).
    /// </summary>
    public static bool IsHealingSpell(this Spell spell)
    {
        int categoryId = spell.GetCategoryId();
        return categoryId switch
        {
            CategoryBeneficialHealingAoe => true,
            CategoryBeneficialHealingTouch => true,
            CategoryBeneficialHealingPotion => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if this is a buff/enhancement spell (for AI buffing behavior).
    /// Includes: Protection, Enhancement, Conditional buffs (Self, Single, AoE).
    /// </summary>
    public static bool IsBuffSpell(this Spell spell)
    {
        int categoryId = spell.GetCategoryId();
        return categoryId switch
        {
            CategoryBeneficialConditionalAoe => true,
            CategoryBeneficialConditionalSingle => true,
            CategoryBeneficialEnhancementAoe => true,
            CategoryBeneficialEnhancementSingle => true,
            CategoryBeneficialEnhancementSelf => true,
            CategoryBeneficialProtectionSelf => true,
            CategoryBeneficialProtectionSingle => true,
            CategoryBeneficialProtectionAoe => true,
            CategoryBeneficialConditionalPotion => true,
            CategoryBeneficialProtectionPotion => true,
            CategoryBeneficialEnhancementPotion => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if this is a summoning spell (for AI summoning behavior).
    /// </summary>
    public static bool IsSummonSpell(this Spell spell)
    {
        int categoryId = spell.GetCategoryId();
        return categoryId == CategoryBeneficialSummon;
    }

    /// <summary>
    /// Checks if this is a persistent area of effect spell.
    /// </summary>
    public static bool IsPersistentAoeSpell(this Spell spell)
    {
        int categoryId = spell.GetCategoryId();
        return categoryId == CategoryPersistentAoe;
    }

    /// <summary>
    /// Checks if this is a dispel/counter spell.
    /// </summary>
    public static bool IsDispelSpell(this Spell spell)
    {
        int categoryId = spell.GetCategoryId();
        return categoryId == CategoryDispel;
    }

    #endregion

    #region Spell Metadata

    /// <summary>
    /// Gets the base caster level for this spell from spells.2da "Innate" column.
    /// </summary>
    public static int GetBaseCasterLevel(this Spell spell)
    {
        string? innateLevel = SpellTable.GetString((int)spell, "Innate");
        if (!string.IsNullOrEmpty(innateLevel) && int.TryParse(innateLevel, out int level))
        {
            return level;
        }
        return 0;
    }

    /// <summary>
    /// Gets the spell school from spells.2da "School" column.
    /// </summary>
    public static int GetSpellSchool(this Spell spell)
    {
        string? school = SpellTable.GetString((int)spell, "School");
        if (!string.IsNullOrEmpty(school) && int.TryParse(school, out int schoolId))
        {
            return schoolId;
        }
        return 0;
    }

    /// <summary>
    /// Gets the spell name from spells.2da "Label" column.
    /// </summary>
    public static string GetSpellName(this Spell spell)
    {
        return SpellTable.GetString((int)spell, "Label") ?? spell.ToString();
    }

    /// <summary>
    /// Gets the target type from spells.2da "TargetType" column.
    /// </summary>
    public static string? GetTargetType(this Spell spell)
    {
        return SpellTable.GetString((int)spell, "TargetType");
    }

    /// <summary>
    /// Gets the range from spells.2da "Range" column.
    /// </summary>
    public static string? GetRange(this Spell spell)
    {
        return SpellTable.GetString((int)spell, "Range");
    }

    /// <summary>
    /// Gets the hostile setting from spells.2da "HostileSetting" column.
    /// </summary>
    public static string? GetHostileSetting(this Spell spell)
    {
        return SpellTable.GetString((int)spell, "HostileSetting");
    }

    #endregion

    #region Target Filtering

    /// <summary>
    /// Checks if this spell is valid for the given target type.
    /// Handles undead-specific spell filtering (cure→harm swap).
    /// </summary>
    public static bool IsValidForTarget(this Spell spell, NwCreature target)
    {
        // Undead-specific filtering (from FixSpellsVersusUndead in ds_ai_include.nss)
        if (target.Race.RacialType == RacialType.Undead)
        {
            // Cure spells harm undead, harm spells heal undead
            if (IsCureSpell(spell)) return true; // Actually damages undead
            if (IsHarmSpell(spell)) return false; // Would heal undead (don't use)
        }

        return true;
    }

    /// <summary>
    /// Checks if this spell requires line of sight to target.
    /// </summary>
    public static bool RequiresLineOfSight(this Spell spell)
    {
        // Most offensive spells require LoS
        return spell.IsAttackSpell();
    }

    #endregion

    #region Spell Type Helpers

    private static bool IsCureSpell(Spell spell)
    {
        return spell switch
        {
            Spell.CureMinorWounds => true,
            Spell.CureLightWounds => true,
            Spell.CureModerateWounds => true,
            Spell.CureSeriousWounds => true,
            Spell.CureCriticalWounds => true,
            Spell.Heal => true,
            Spell.Regenerate => true,
            _ => false
        };
    }

    private static bool IsHarmSpell(Spell spell)
    {
        return spell switch
        {
            Spell.InflictMinorWounds => true,
            Spell.InflictLightWounds => true,
            Spell.InflictModerateWounds => true,
            Spell.InflictSeriousWounds => true,
            Spell.InflictCriticalWounds => true,
            Spell.Harm => true,
            _ => false
        };
    }

    #endregion
}

