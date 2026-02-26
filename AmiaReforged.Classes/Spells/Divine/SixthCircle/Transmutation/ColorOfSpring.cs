using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.SixthCircle.Transmutation;

/// <summary>
/// Level: Druid 6
/// Area of effect: Huge
/// Valid Metamagic: Still, Extend, Silent, Empower, Maximize
/// Save: Special
/// Spell Resistance: Yes
/// This spell brings new beginnings to those around the caster. All natural living allies
/// (not undead, construct, outsider, or aberration) receive restoration and are healed for 1d8 + caster level hit points.
/// Enemy undead must make a will save or be turned for 1d6 rounds.
/// Enemy aberrations must make a will save or be stunned for 1d6 rounds.
/// Enemy constructs must make a fortitude save or be knocked down for 1d6 rounds.
/// Enemy outsiders must make a fortitude save or be dazed for 1d6 rounds.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class ColorOfSpring : ISpell
{
    private const VfxType FnfSpringColor = (VfxType)2550;
    public string ImpactScript => "color_of_spring";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster || eventData.TargetLocation is not {} location) return;

        Effect restorationVfx = Effect.VisualEffect(VfxType.ImpRestoration);
        Effect fortitudeVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);
        Effect willVfx = Effect.VisualEffect(VfxType.ImpWillSavingThrowUse);
        Effect impVfx = Effect.VisualEffect(VfxType.ImpSunstrike);
        Effect turnEffect = Effect.LinkEffects(Effect.VisualEffect(VfxType.DurMindAffectingFear), Effect.Turned());
        Effect stunEffect = Effect.Stunned();
        Effect knockdownEffect = Effect.Knockdown();
        Effect dazeEffect = Effect.LinkEffects(Effect.VisualEffect(VfxType.DurMindAffectingNegative), Effect.Dazed());

        MetaMagic metaMagic = eventData.MetaMagicFeat;

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(FnfSpringColor));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, false))
        {
            if (caster.IsReactionTypeFriendly(creature) && IsNatural(creature))
                _ = DoRestoration(caster, creature, caster.CasterLevel, metaMagic, restorationVfx);

            if (!caster.IsReactionTypeHostile(creature) || IsNatural(creature) ||
                caster.SpellResistanceCheck(creature, eventData.Spell, caster.CasterLevel)) continue;

            _ = DoBadEffect(caster, creature, eventData.SaveDC, metaMagic, fortitudeVfx, willVfx, impVfx, turnEffect,
                stunEffect, knockdownEffect, dazeEffect);
        }
    }

    private static async Task DoBadEffect(NwCreature caster, NwCreature creature, int dc, MetaMagic metaMagic,
        Effect fortitudeVfx, Effect willVfx, Effect impVfx, Effect turn, Effect stun, Effect knockdown, Effect daze)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(1.5, 2.0));
        if (creature.IsDead || !creature.IsValid) return;

        await caster.WaitForObjectContext();

        TimeSpan duration = NwTimeSpan.FromRounds(Random.Shared.Roll(6));
        if (metaMagic == MetaMagic.Extend) duration *= 2;

        switch (creature.Race.RacialType)
        {
            case RacialType.Undead :
                if (creature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.None, caster) == SavingThrowResult.Success)
                    creature.ApplyEffect(EffectDuration.Instant, willVfx);
                else
                {
                    creature.ApplyEffect(EffectDuration.Temporary, turn, duration);
                    creature.ApplyEffect(EffectDuration.Instant, impVfx);
                }
                break;

            case RacialType.Aberration :
                if (creature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.None, caster) == SavingThrowResult.Success)
                    creature.ApplyEffect(EffectDuration.Instant, willVfx);
                else
                {
                    creature.ApplyEffect(EffectDuration.Temporary, stun, duration);
                    creature.ApplyEffect(EffectDuration.Instant, impVfx);
                }
                break;

            case RacialType.Construct :
                if (creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, caster) == SavingThrowResult.Success)
                    creature.ApplyEffect(EffectDuration.Instant, fortitudeVfx);
                else
                {
                    creature.ApplyEffect(EffectDuration.Temporary, knockdown, duration);
                    creature.ApplyEffect(EffectDuration.Instant, impVfx);
                }
                break;

            case RacialType.Outsider :
                if (creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, caster) == SavingThrowResult.Success)
                    creature.ApplyEffect(EffectDuration.Instant, fortitudeVfx);
                else
                {
                    creature.ApplyEffect(EffectDuration.Temporary, daze, duration);
                    creature.ApplyEffect(EffectDuration.Instant, impVfx);
                }
                break;
        }
    }

    private static bool IsNatural(NwCreature creature) =>
        creature.Race.RacialType is not (RacialType.Undead or RacialType.Aberration or RacialType.Construct or RacialType.Outsider);

    private static async Task DoRestoration(NwCreature caster, NwCreature creature, int casterLevel,
        MetaMagic metaMagic, Effect restorationVfx)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(1.5, 2.0));
        if (creature.IsDead || !creature.IsValid) return;

        await caster.WaitForObjectContext();

        creature.ApplyEffect(EffectDuration.Instant, restorationVfx);

        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType is EffectType.AbilityDecrease or EffectType.AcDecrease or EffectType.DamageDecrease
                    or EffectType.DamageImmunityDecrease or EffectType.SavingThrowDecrease or EffectType.SkillDecrease
                    or EffectType.Blindness or EffectType.Deaf or EffectType.Paralyze or EffectType.NegativeLevel or
                    EffectType.AttackDecrease
                && effect.SubType != EffectSubType.Unyielding)

                creature.RemoveEffect(effect);
        }

        int healAmount = SpellUtils.MaximizeSpell(metaMagic, 8, 1) + casterLevel;
        healAmount = SpellUtils.EmpowerSpell(metaMagic, healAmount);

        creature.ApplyEffect(EffectDuration.Instant, Effect.Heal(healAmount));
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
