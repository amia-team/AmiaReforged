using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Monster;

/**
 * Range: personal
 * Save: fortitude
 * Spell resistance: no
 * Descriptor: disease
 * Area of effect: 4 meters around creature
 * Duration: hour / level
 *
 * Description: Rotting Aura causes living creatures that enter it to be sickened until they leave the aura.
 * A sickened creature takes a -2 penalty to their attack, damage, skill, and saving throws.
 * Further, each round that a creature lingers in the aura, they must pass a fortitude save or be nauseated (slowed)
 * until they leave the aura. A nauseated creature takes a -2 penalty to armor class and constitution,
 * and their movement speed is halved. Each time a creature fails the saving throw, the aura's creator is healed for 5 damage.
 */

[ServiceBinding(typeof(ISpell))]
public class RottingAura(ShifterDcService shifterDcService, ScriptHandleFactory scriptHandleFactory) : ISpell
{
    private const PersistentVfxType MobRottingVfx = (PersistentVfxType)57;
    private const string SickenedEffectTag = "sickened_rotting";
    private const string NauseatedEffectTag = "nauseated_rotting";
    private const string RottingAuraEffectTag = "rotting_aura";

    private static readonly Effect SickenedVfx = Effect.VisualEffect(VfxType.ImpDiseaseS);
    private static readonly Effect NauseatedVfx = Effect.VisualEffect((VfxType)2517);
    private static readonly Effect SyphonHeal = Effect.Heal(5);

    private static Effect SickenedEffect()
    {
        Effect sickenedEffect =
        Effect.LinkEffects
        (
            Effect.AttackDecrease(2),
            Effect.DamageDecrease(2, DamageType.Slashing),
            Effect.SkillDecreaseAll(2),
            Effect.SavingThrowDecrease(SavingThrow.All, 2)
        );
        sickenedEffect.SubType = EffectSubType.Extraordinary;
        sickenedEffect.Tag = SickenedEffectTag;

        return sickenedEffect;
    }

    private static Effect NauseatedEffect()
    {
        Effect nauseatedEffect = Effect.LinkEffects
        (
            Effect.ACDecrease(2),
            Effect.MovementSpeedDecrease(50),
            Effect.AbilityDecrease(Ability.Constitution, 2)
        );

        nauseatedEffect.SubType = EffectSubType.Extraordinary;
        nauseatedEffect.Tag = NauseatedEffectTag;

        return nauseatedEffect;
    }


    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "rotting_aura";


    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;

        // Recasting disables aura instead of stacking it
        Effect? existingAura = eventData.Caster.ActiveEffects.FirstOrDefault(e => e.Tag == RottingAuraEffectTag);
        if (existingAura != null)
        {
            eventData.Caster.RemoveEffect(existingAura);
            return;
        }

        int casterLevel = eventData.Caster.CasterLevel;
        int dc = eventData.SaveDC;

        if (eventData.Caster is NwCreature casterCreature)
        {
            casterLevel = shifterDcService.GetShifterCasterLevel(casterCreature, casterLevel);
            dc = shifterDcService.GetShifterDc(casterCreature, dc);
        }

        TimeSpan duration = NwTimeSpan.FromHours(casterLevel);

        Effect? rottingAuraEffect = RottingAuraEffect(eventData.Caster, dc);
        if (rottingAuraEffect == null) return;

        eventData.Caster.ApplyEffect(EffectDuration.Temporary, rottingAuraEffect, duration);
    }

    public void SetSpellResisted(bool result)
    {
        // This spell doesn't allow Spell Resistance
    }

    private Effect? RottingAuraEffect(NwGameObject caster, int dc)
    {
        PersistentVfxTableEntry? rottingAuraVfx = MobRottingVfx;
        if (rottingAuraVfx == null)
        {
            if (caster.IsPlayerControlled(out NwPlayer? player))
                player.SendServerMessage("No vfx for Rotting Aura found! Please make a bug report.");
            return null;
        }

        // The appearance type is checked in case of polymorph
        AppearanceTableEntry? appearanceType = null;
        if (caster is NwCreature casterCreature)
        {
            appearanceType = casterCreature.Appearance;
        }

        ScriptCallbackHandle rottingAuraEnter =
            scriptHandleFactory.CreateUniqueHandler(info
                => OnEnterRottingAura(info, caster, appearanceType));

        ScriptCallbackHandle rottingAuraHeartbeat =
            scriptHandleFactory.CreateUniqueHandler(info
                => OnHeartbeatRottingAura(info, caster, appearanceType, dc));

        ScriptCallbackHandle rottingAuraExit = scriptHandleFactory.CreateUniqueHandler(OnExitRottingAura);

        Effect rottingAuraEffect =
            Effect.AreaOfEffect(rottingAuraVfx, rottingAuraEnter, rottingAuraHeartbeat, rottingAuraExit);
        rottingAuraEffect.SubType = EffectSubType.Extraordinary;
        rottingAuraEffect.Tag = RottingAuraEffectTag;

        return rottingAuraEffect;
    }

    private static ScriptHandleResult OnEnterRottingAura(CallInfo info, NwGameObject caster,
        AppearanceTableEntry? appearanceType)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature targetCreature
            || !IsValidCreature(targetCreature))
            return ScriptHandleResult.Handled;

        if (caster is NwCreature casterCreature)
        {
            if (casterCreature.Appearance != appearanceType)
            {
                RemoveRottingAura(casterCreature);
                return ScriptHandleResult.Handled;
            }

            if (casterCreature.IsReactionTypeFriendly(targetCreature))
                return ScriptHandleResult.Handled;
        }

        targetCreature.ApplyEffect(EffectDuration.Instant, SickenedVfx);
        targetCreature.ApplyEffect(EffectDuration.Permanent, SickenedEffect());

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatRottingAura(CallInfo info, NwGameObject caster,
        AppearanceTableEntry? appearanceType, int dc)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        if (caster is NwCreature casterCreature && appearanceType != casterCreature.Appearance)
        {
            RemoveRottingAura(casterCreature);
            return ScriptHandleResult.Handled;
        }

        foreach (NwCreature targetCreature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (!IsValidCreature(targetCreature)) continue;

            if (caster is NwCreature creature && creature.IsReactionTypeFriendly(targetCreature)) continue;

            SavingThrowResult savingThrowResult
                = targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Disease, caster);

            if (savingThrowResult != SavingThrowResult.Failure) continue;

            targetCreature.ApplyEffect(EffectDuration.Permanent, NauseatedEffect());
            targetCreature.ApplyEffect(EffectDuration.Instant, NauseatedVfx);

            caster.ApplyEffect(EffectDuration.Instant, SyphonHeal);
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnExitRottingAura(CallInfo info)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnExit? eventData)
            || eventData.Exiting is not NwCreature targetCreature)
            return ScriptHandleResult.Handled;

        Effect? sickened = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == SickenedEffectTag);
        if (sickened != null) targetCreature.RemoveEffect(sickened);

        Effect? nauseated = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == NauseatedEffectTag);
        if (nauseated != null) targetCreature.RemoveEffect(nauseated);

        return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// // Only affects living creatures that aren't immune to disease
    /// </summary>
    private static bool IsValidCreature(NwCreature targetCreature) =>
        targetCreature.Race.RacialType is not (RacialType.Ooze or RacialType.Construct or RacialType.Elemental or RacialType.Undead)
        && !targetCreature.IsImmuneTo(ImmunityType.Disease);

    private static void RemoveRottingAura(NwGameObject caster)
    {
        Effect? aura = caster.ActiveEffects.FirstOrDefault(e => e.Tag == RottingAuraEffectTag);
        if (aura != null) caster.RemoveEffect(aura);
    }
}
