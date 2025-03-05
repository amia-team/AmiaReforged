using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.BallOfSound;

[ServiceBinding(typeof(ISpell))]
public class BallOfSound : ISpell
{
    public ResistSpellResult Result { get; set; }
    public string ImpactScript => "amx_csp_bsound";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject == null) return;

        Task<TouchAttackResult> result = casterCreature.TouchAttackRanged(eventData.TargetObject, true);

        if (result.Result != TouchAttackResult.Hit) return;

        int damage = CalculateDamage(casterCreature);

        if (Result != ResistSpellResult.Failed || eventData.TargetObject.ActiveEffects.Any(e => e.EffectType == EffectType.Deaf)) return;

        ApplyEffect(eventData, damage);
    }

    private int CalculateDamage(NwCreature casterCreature)
    {
        int numDie = casterCreature.CasterLevel / 2;

        return NWScript.d3(numDie);
    }

    private void ApplyEffect(SpellEvents.OnSpellCast eventData, int damage)
    {
        Effect damageEffect = Effect.Damage(damage, DamageType.Sonic);
        Effect impactVfx = Effect.VisualEffect(VfxType.ImpSonic);

        damageEffect = Effect.LinkEffects(damageEffect, impactVfx);

        eventData.TargetObject!.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }
}