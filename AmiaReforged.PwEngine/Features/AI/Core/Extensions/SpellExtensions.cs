using AmiaReforged.PwEngine.Features.AI.Core.Models;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Extensions;

/// <summary>
/// Extension methods for Spell providing categorization and filtering.
/// Uses 2DA data from spells.2da and categories.2da for spell metadata.
/// Category IDs reference categories.2da (1-23), not simple 0-3 values.
/// </summary>
public static class SpellExtensions
{
    // Amia custom ID for dispel talent not found in the normal Anvil.API
    private const TalentCategory TalentCategoryDispel = (TalentCategory)23;

    /// <summary>
    /// Gets the spell talent type based on the spell's category.
    /// </summary>
    public static SpellTalentType GetSpellTalent(this NwSpell spell) => spell.TalentCategory switch
    {
        TalentCategory.HarmfulAreaEffectDiscriminant or TalentCategory.HarmfulRanged or TalentCategory.HarmfulTouch
            or TalentCategory.HarmfulAreaEffectIndiscriminant or TalentCategory.HarmfulMelee or TalentCategory.DragonsBreath
            => SpellTalentType.Attack,
        TalentCategory.BeneficialConditionalAreaEffect or TalentCategory.BeneficialConditionalSingle
            or TalentCategory.BeneficialEnhancementSingle or TalentCategory.BeneficialEnhancementAreaEffect
            or TalentCategory.BeneficialEnhancementSelf or TalentCategory.BeneficialProtectionSelf
            or TalentCategory.BeneficialProtectionSingle or TalentCategory.BeneficialProtectionAreaEffect
            or TalentCategory.BeneficialConditionalPotion or TalentCategory.BeneficialProtectionPotion
            or TalentCategory.BeneficialEnhancementPotion
            => SpellTalentType.Buff,
        TalentCategory.BeneficialHealingAreaEffect or TalentCategory.BeneficialHealingTouch
            or TalentCategory.BeneficialHealingPotion
            => SpellTalentType.Heal,
        TalentCategory.BeneficialObtainAllies
            => SpellTalentType.Summon,
        TalentCategory.PersistentAreaOfEffect
            => SpellTalentType.PersistentAoe,
        TalentCategoryDispel
            => SpellTalentType.Dispel,
        _ => SpellTalentType.Unknown
    };

    /// <summary>
    /// Checks if this spell is valid for the given target type.
    /// Handles undead-specific spell filtering (cure -> harm swap).
    /// </summary>
    public static bool IsValidForTarget(this NwSpell spell, NwCreature target)
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

    public static bool TryGetRandomPolymorphSpell(this NwSpell spell, out NwSpell? polymorphSpell)
    {
        polymorphSpell = spell.MasterSpell switch
        {
            { SpellType: Spell.PolymorphSelf } => NwSpell.FromSpellId(GetRandomPolymorphShape()),
            { SpellType: Spell.Shapechange } => NwSpell.FromSpellId(GetRandomShapechangeShape()),
            _ => null
        };

        return polymorphSpell != null;
    }

    private static int GetRandomPolymorphShape() => Random.Shared.Next(387, 392);
    private static int GetRandomShapechangeShape() => Random.Shared.Next(392, 401);

    /// <summary>
    /// Checks if this spell requires line of sight to target.
    /// </summary>
    public static bool RequiresLineOfSight(this NwSpell spell)
        => spell.Range is not (SpellRange.Personal or SpellRange.Touch);


    #region Spell Type Helpers

    private static bool IsCureSpell(NwSpell spell) => spell.SpellType switch
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

    private static bool IsHarmSpell(NwSpell spell) => spell.SpellType switch
    {
        Spell.InflictMinorWounds => true,
        Spell.InflictLightWounds => true,
        Spell.InflictModerateWounds => true,
        Spell.InflictSeriousWounds => true,
        Spell.InflictCriticalWounds => true,
        Spell.Harm => true,
        _ => false
    };

    /// <summary>
    /// Gets the spell priority based on the spell's innate level. Certain good spells are given higher priority.
    /// </summary>
    public static int GetSpellPriority(this NwSpell spell) => spell.SpellType switch
    {
        Spell.TrueStrike => 10,
        Spell.GhostlyVisage => 8,
        Spell.Stoneskin => 8,
        Spell.EtherealVisage => 8,
        Spell.Haste => 7,
        Spell.ImprovedInvisibility => 7,
        Spell.Displacement => 7,
        Spell.Darkness => 6,
        Spell.Darkvision => 5,  // This is actually Ultravision
        Spell.Invisibility => 4,
        Spell.SeeInvisibility => 4,
        _ => spell.InnateSpellLevel
    };

    #endregion
}

