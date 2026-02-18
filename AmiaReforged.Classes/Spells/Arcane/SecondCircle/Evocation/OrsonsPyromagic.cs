using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Evocation;

/// <summary>
///  The caster surrounds themselves with flaming wisps. Every round, any enemies who get within 5 feet of the caster
/// are struck by the wisps. They must make a reflex save or take 1d6 + 1 per caster level fire damage,
/// to a maximum of 1d6+10 fire damage. A passed reflex save negates the damage completely.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class OrsonsPyromagic(ScriptHandleFactory scriptHandleFactory) : ISpell
{
    private static readonly VfxType DurFireWhirl = (VfxType)2545;
    private static readonly VfxType ImpMirvFire = (VfxType)2544;

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "orsons_pyro";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster || caster.Location == null) return;

        MetaMagic metaMagic = eventData.MetaMagicFeat;

        TimeSpan duration = NwTimeSpan.FromRounds(eventData.Caster.CasterLevel);
        if (metaMagic == MetaMagic.Extend)
            duration *= 2;

        int dc = eventData.SaveDC;

        Effect pyromagicEffect = CreatePyromagicEffect(caster, dc, metaMagic, eventData.Spell);

        caster.ApplyEffect(EffectDuration.Temporary, pyromagicEffect, duration);
    }

    private Effect CreatePyromagicEffect(NwCreature caster, int dc, MetaMagic metaMagic, NwSpell spell)
    {
        ScriptCallbackHandle doPyro
            = scriptHandleFactory.CreateUniqueHandler(_ => DoPyro(caster, dc, metaMagic, spell));

        Effect pyroEffect = Effect.LinkEffects
        (
            Effect.RunAction(onIntervalHandle: doPyro, interval: NwTimeSpan.FromRounds(1)),
            Effect.VisualEffect(DurFireWhirl)
        );
        pyroEffect.SubType = EffectSubType.Magical;

        return pyroEffect;
    }


    private ScriptHandleResult DoPyro(NwCreature caster, int dc, MetaMagic metaMagic, NwSpell spell)
    {
        if (caster.Location == null) return ScriptHandleResult.True;

        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
        Effect damageVfx = Effect.VisualEffect(VfxType.ImpFlameS);

        foreach (NwCreature creature in caster.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Large, true))
        {
            if (!caster.IsReactionTypeHostile(creature) || !creature.IsValid || creature.IsDead) continue;

            creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(ImpMirvFire));
            _ = ApplyFireMissile(caster, creature, dc, metaMagic, spell, reflexVfx, damageVfx);
        }

        return ScriptHandleResult.True;
    }

    private static async Task ApplyFireMissile(NwCreature caster, NwCreature target, int dc,
        MetaMagic metaMagic, NwSpell spell, Effect reflexVfx, Effect damageVfx)
    {
        float distanceToTarget = caster.Distance(target);
        float missileTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 2f);

        await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));

        if (!target.IsValid || target.IsDead) return;
        if (caster.SpellResistanceCheck(target, spell, caster.CasterLevel)) return;
        if (target.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Fire, caster)
            == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, reflexVfx);
            return;
        }

        int bonusDamage = Math.Min(10, caster.CasterLevel);
        int damageRoll = SpellUtils.MaximizeSpell(metaMagic, 6, 1) + bonusDamage;
        damageRoll = SpellUtils.EmpowerSpell(metaMagic, damageRoll);

        await caster.WaitForObjectContext();
        Effect damage = Effect.Damage(damageRoll, DamageType.Fire);

        target.ApplyEffect(EffectDuration.Instant, damage);
        target.ApplyEffect(EffectDuration.Instant, damageVfx);
    }

    public void SetSpellResisted(bool result)
    {
        // This spell checks for each spell resist individually
    }
}
