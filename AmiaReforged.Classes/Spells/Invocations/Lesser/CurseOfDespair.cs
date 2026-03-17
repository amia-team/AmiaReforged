using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

[ServiceBinding(typeof(IInvocation))]
public class CurseOfDespair : IInvocation
{
    public string ImpactScript => "wlk_curse";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        Effect curse = Effect.LinkEffects
        (
            Effect.Curse(3, 3, 3, 3, 3, 3),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        curse.SubType = EffectSubType.Supernatural;

        Effect abDecrease = Effect.LinkEffects
        (
            Effect.AttackDecrease(1),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        abDecrease.SubType = EffectSubType.Supernatural;

        Effect curseImpVfx = Effect.VisualEffect(VfxType.ImpReduceAbilityScore);
        Effect abDecreaseImpVfx = Effect.VisualEffect(VfxType.ImpHeadEvil);

        int invocationDc = warlock.InvocationDc(invocationCl);
        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPulseNegative));

        foreach (NwCreature creature in
                 location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, true))
        {
            if (!creature.IsValidInvocationTarget(warlock, false)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, castData.Spell);

            SavingThrowResult willSave =
                creature.RollSavingThrow(SavingThrow.Will, invocationDc, SavingThrowType.None, warlock);

            if (willSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, abDecreaseImpVfx);
                creature.ApplyEffect(EffectDuration.Temporary, abDecrease, duration);
                continue;
            }

            creature.ApplyEffect(EffectDuration.Instant, curseImpVfx);
            creature.ApplyEffect(EffectDuration.Temporary, curse, duration);
        }

    }
}
