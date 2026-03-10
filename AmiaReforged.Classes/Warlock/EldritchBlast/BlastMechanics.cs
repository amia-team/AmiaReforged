using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using NWN.Core.NWNX;
using static AmiaReforged.Classes.Warlock.Constants.WarlockFeats;

namespace AmiaReforged.Classes.Warlock.EldritchBlast;

public static class BlastMechanics
{
    public static void ApplyEldritchBlast(this NwGameObject targetObject, NwCreature warlock, int damage,
        int invocationDc, EssenceData essence)
    {
        if (targetObject.IsValid)
        {
            targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(essence.DmgImpVfx));
            targetObject.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, essence.DamageType));
        }

        if (targetObject is not NwCreature targetCreature || essence.Effect == null) return;

        SavingThrowResult savingThrow =
            targetCreature.RollSavingThrow(essence.SavingThrow, invocationDc, essence.SavingThrowType, warlock);

        if (savingThrow == SavingThrowResult.Success)
        {
            PlaySavingThrowVfx(targetCreature, essence.SavingThrow);
            return;
        }

        if (savingThrow != SavingThrowResult.Failure) return;

        ApplyEssenceEffect(targetCreature, essence.Type, essence.Effect, essence.AllowStacking, essence.Duration,
            essence.EffectImpVfx);
    }

    private static void ApplyEssenceEffect(NwCreature targetCreature, EssenceType essenceType, Effect essenceEffect,
        bool allowStacking, TimeSpan? duration, VfxType? effectImpVfx)
    {
        essenceEffect.SubType = EffectSubType.Magical;
        essenceEffect.Tag = essenceType.ToString();

        if (!allowStacking)
        {
            targetCreature.RemoveExistingEssenceEffect(essenceType);
        }

        if (duration == null)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, essenceEffect);
        }
        else
        {
            targetCreature.ApplyEffect(EffectDuration.Temporary, essenceEffect, duration.Value);
        }

        if (effectImpVfx != null)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(effectImpVfx.Value));
    }

    private static void RemoveExistingEssenceEffect(this NwCreature targetCreature, EssenceType essenceType)
    {
        Effect? existingEssenceEffect
            = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == essenceType.ToString());

        if (existingEssenceEffect != null)
            targetCreature.RemoveEffect(existingEssenceEffect);
    }

    private static void PlaySavingThrowVfx(NwCreature targetCreature, SavingThrow savingThrow)
    {
        VfxType savingThrowVfx = savingThrow switch
        {
            SavingThrow.Fortitude => VfxType.ImpFortitudeSavingThrowUse,
            SavingThrow.Reflex => VfxType.ImpReflexSaveThrowUse,
            _ => VfxType.ImpWillSavingThrowUse
        };
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(savingThrowVfx));
    }

    /// <summary>
    /// Rolls damage for an Eldritch Blast and applies all modifiers.
    /// </summary>
    /// <param name="damageModifiers"></param>
    /// <param name="warlockLevel"></param>
    /// <param name="touchAttackResult">Used to double the damage on critical hits of touch attacks</param>
    /// <returns>The total damage to Eldritch Blast applied with ApplyEldritchBlast</returns>
    public static int RollEldritchDamage((int FlatBonus, double Multiplier) damageModifiers, int warlockLevel,
        TouchAttackResult? touchAttackResult = null)
    {
        double damage = Random.Shared.Roll(2, warlockLevel);
        damage += damageModifiers.FlatBonus;
        damage *= damageModifiers.Multiplier;

        if (touchAttackResult == TouchAttackResult.CriticalHit) damage *= 2;

        return (int)damage;
    }

    public static (int FlatBonus, double Multiplier) GetEldritchDamageModifiers(NwCreature warlock, int warlockLevel)
    {
        // Every Epic Eldritch Blast feat increases the damage by 5.
        int epicEldritchCount = EpicEldritchFeatCount(warlock);
        int flatBonus = epicEldritchCount * 5;

        double multiplier = 1.0;

        // Charisma multiplies the damage by 1% per Cha Mod, multiplied by 1.5 if warlock is level 30
        int chaMod = warlock.GetAbilityModifier(Ability.Charisma);
        double chaScale = warlockLevel == 30 ? 1.5 : 1.0;
        multiplier += chaMod * chaScale / 100;

        // Eldritch Master increases the damage by 25%
        if (warlock.KnowsFeat(EldritchMaster!))
            multiplier += 0.25;

        return (flatBonus, multiplier);
    }

    /// <summary>
    /// Eldritch Master grants +3 nonstacking AB every time Eldritch Blast is cast.
    /// </summary>
    public static void ApplyEldritchMasterAttackBonus(this NwCreature warlock)
    {
        Effect? eldritchMasterEffect = warlock.ActiveEffects.FirstOrDefault(e => e.Tag == EldritchMasterEffectTag);

        if (eldritchMasterEffect != null)
            warlock.RemoveEffect(eldritchMasterEffect);

        eldritchMasterEffect = Effect.AttackIncrease(3);
        eldritchMasterEffect.SubType = EffectSubType.Magical;
        eldritchMasterEffect.Tag = EldritchMasterEffectTag;

        warlock.ApplyEffect(EffectDuration.Temporary, eldritchMasterEffect, NwTimeSpan.FromRounds(1));
    }

    private const string EldritchMasterEffectTag = "eldritch_master_effect";

    private static int EpicEldritchFeatCount(NwCreature warlock)
    {
        int highestLevel = CreaturePlugin.GetHighestLevelOfFeat(warlock, (int)EpicEldritchBlast1);

        return highestLevel switch
        {
            1300 => 1,
            1301 => 2,
            1302 => 3,
            1303 => 4,
            1304 => 5,
            1305 => 6,
            1306 => 7,
            _ => 0
        };
    }

    /// <summary>
    /// Performs ranged or melee touch attack and adjusts the attack result if target object is immune to critical hits.
    /// </summary>
    /// <param name="warlock">The warlock casting the Eldritch Blast</param>
    /// <param name="targetObject">The target of the attack.</param>
    /// <param name="ranged">Default is true to perform a ranged touch attack. Set to false for melee.</param>
    /// <returns>
    /// TouchAttackResult, with Crit converted to Hit if target is immune to crit.
    /// </returns>
    public static TouchAttackResult EldritchTouchAttack(this NwCreature warlock, NwGameObject targetObject, bool ranged = true)
    {
        TouchAttackResult result = ranged
            ? warlock.TouchAttackRanged(targetObject, true)
            : warlock.TouchAttackMelee(targetObject);

        if (targetObject is NwCreature creature && result == TouchAttackResult.CriticalHit &&
            creature.IsImmuneTo(ImmunityType.CriticalHit))
            result = TouchAttackResult.Hit;

        return result;
    }
}
