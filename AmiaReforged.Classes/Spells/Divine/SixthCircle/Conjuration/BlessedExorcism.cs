using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.SixthCircle.Conjuration;

/// <summary>
/// Level: Cleric 2
/// Area of effect: Colossal
/// Valid Metamagic: Still, Extend, Silent
/// Save: Will negates
/// Spell Resistance: Yes
///  The cleric exorcises the influence of spirits opposed to their patron.  All allies in the area of effect are freed
///  from mind-affecting maladies, fear, daze, domination, stun, or other mind effects.
///  Any hostile undead or outsiders in the area of effect must make a will save or be turned for 1d6+1 rounds.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class BlessedExorcism : ISpell
{
    private static readonly EffectType[] MindAffectingEffectTypes =
    [
        EffectType.Stunned, EffectType.Dominated, EffectType.Dazed, EffectType.Frightened, EffectType.Confused,
        EffectType.Charmed, EffectType.Pacify, EffectType.Sleep, EffectType.Turned
    ];
    public string ImpactScript => "blessed_exorcism";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not NwCreature caster || caster.Location == null) return;

        Effect positiveVfx = Effect.VisualEffect(VfxType.ImpRemoveCondition);
        Effect willVfx = Effect.VisualEffect(VfxType.ImpWillSavingThrowUse);
        Effect turnEffect =
            Effect.LinkEffects(Effect.VisualEffect(VfxType.DurMindAffectingFear), Effect.Turned());

        caster.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfWord, fScale: 0.5f));

        foreach (NwCreature creature in caster.Location.GetObjectsInShapeByType<NwCreature>
                     (Shape.Sphere, RadiusSize.Large, false))
        {
            if (caster.IsReactionTypeFriendly(creature))
            {
                _ = RemoveMindAffectingMaladies(caster, creature, positiveVfx);
                continue;
            }

            if (!caster.IsReactionTypeHostile(creature) ||
                creature.Race.RacialType is not (RacialType.Undead or RacialType.Outsider)) continue;

            _ = TryToTurn(caster, creature, eventData.Spell, eventData.SaveDC, willVfx, turnEffect, eventData.MetaMagicFeat);
        }
    }

    private static async Task TryToTurn(NwCreature caster, NwCreature creature, NwSpell spell, int dc, Effect willVfx,
        Effect turnEffect, MetaMagic metaMagic)
    {
        await Task.Delay(SpellUtils.GetRandomDelay(0.5, 2));
        await caster.WaitForObjectContext();

        if (!creature.IsValid || creature.IsDead) return;

        CreatureEvents.OnSpellCastAt.Signal(caster, creature, spell);
        if (caster.SpellResistanceCheck(creature, spell, caster.CasterLevel)) return;
        if (creature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Divine, caster)
            == SavingThrowResult.Success)
        {
            creature.ApplyEffect(EffectDuration.Instant, willVfx);
            return;
        }

        TimeSpan turnDuration = NwTimeSpan.FromRounds(Random.Shared.Roll(6) + 1);
        if (metaMagic == MetaMagic.Extend) turnDuration *= 2;

        creature.ApplyEffect(EffectDuration.Temporary, turnEffect, turnDuration);
    }

    private static async Task RemoveMindAffectingMaladies(NwCreature caster, NwCreature creature, Effect positiveVfx)
    {
        await Task.Delay(SpellUtils.GetRandomDelay(0.5, 2));
        await caster.WaitForObjectContext();

        if (!creature.IsValid || creature.IsDead) return;

        bool effectRemoved = false;
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (!MindAffectingEffectTypes.Contains(effect.EffectType)) continue;

            creature.RemoveEffect(effect);

            if (effectRemoved) continue;
            creature.ApplyEffect(EffectDuration.Instant, positiveVfx);
            effectRemoved = true;
        }
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
