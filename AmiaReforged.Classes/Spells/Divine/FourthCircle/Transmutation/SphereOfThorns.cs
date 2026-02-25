using AmiaReforged.Classes.Poisons;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Org.BouncyCastle.Crypto.Engines;

namespace AmiaReforged.Classes.Spells.Divine.FourthCircle.Transmutation;

/// <summary>
/// /// <summary>
/// Level: Druid 4
/// Area of effect: Large
/// Duration: 1 Round/2 Caster Level
/// Valid Metamagic: Still, Extend, Silent
/// Save: Reflex Negates, Fortitude Partial
/// Spell Resistance: Yes
///  This spell casts a globe of ravaging plants to assail their foes. Any enemies caught in the spell's radius must
///  make a reflex save or be entangled. The victims can make further saves to escape the entanglement.
///  Any targets hit by the entanglement must make a fortitude save or be poisoned.
///  The poison does 1d6 points of primary dexterity damage and 2d6 secondary dexterity damage.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class SphereOfThorns(PoisonService poisonService) : ISpell
{
    private const VfxType FnfThornSphere = (VfxType)2549;
    public string ImpactScript => "sphere_of_thorns";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster || eventData.TargetLocation is not { } location) return;

        Effect reflexSave = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
        Effect entangleEffect = Effect.LinkEffects(Effect.Entangle(), Effect.VisualEffect(VfxType.DurEntangle));
        entangleEffect.SubType = EffectSubType.Magical;

        TimeSpan entangleDuration = NwTimeSpan.FromRounds(caster.CasterLevel / 2);
        if (eventData.MetaMagicFeat == MetaMagic.Extend) entangleDuration *= 2;

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(FnfThornSphere, fScale: 1.5f));
        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, true))
        {
            _ = ApplyEffect(caster, creature, eventData.SaveDC, reflexSave, entangleEffect, entangleDuration, eventData.Spell);
        }
    }

    private async Task ApplyEffect(NwCreature caster, NwCreature creature, int dc, Effect reflexSave, Effect entangleEffect,
        TimeSpan entangleDuration, NwSpell spell)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(0.4, 0.8));

        await caster.WaitForObjectContext();

        if (!caster.IsReactionTypeHostile(creature) || creature.IsDead || !creature.IsValid) return;

        CreatureEvents.OnSpellCastAt.Signal(caster, creature, spell);
        caster.SpellResistanceCheck(creature, spell, caster.CasterLevel);

        SavingThrowResult savingThrowResult =
            creature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Spell, caster);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Success:
                creature.ApplyEffect(EffectDuration.Instant, reflexSave);
                break;
            case SavingThrowResult.Failure:
                creature.ApplyEffect(EffectDuration.Temporary, entangleEffect, entangleDuration);
                poisonService.ApplyPoisonEffect(PoisonType.TerinavRoot, creature, caster, dc);
                break;
        }
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
