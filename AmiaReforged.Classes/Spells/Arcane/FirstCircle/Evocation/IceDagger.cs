using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class IceDagger : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X2_S0_IceDagg";

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        SpellUtils.SignalSpell(casterCreature, target, eventData.Spell);

        if (ResistedSpell) return;

        int casterLevel = casterCreature.CasterLevel;
        int damage = SpellUtils.MaximizeSpell(eventData.MetaMagicFeat, 4, casterLevel / 2);
        damage = SpellUtils.EmpowerSpell(eventData.MetaMagicFeat, damage);

        // Reflex save for half damage (handles evasion and improved evasion)
        int dc = SpellUtils.GetSpellDc(eventData);
        int adjustedDamage = NWScript.GetReflexAdjustedDamage(damage, target, dc, NWScript.SAVING_THROW_TYPE_COLD, casterCreature);

        if (adjustedDamage <= 0) return;

        Effect damageEffect = Effect.Damage(adjustedDamage, DamageType.Cold);
        Effect vfx = Effect.VisualEffect(VfxType.ImpFrostS);

        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        target.ApplyEffect(EffectDuration.Instant, vfx);

        // Epic Spell Focus Evocation: cold pulse AoE around the target
        if (!casterCreature.KnowsFeat(Feat.EpicSpellFocusEvocation!)) return;
        if (target.Location is not { } targetLocation) return;

        Effect pulseVfx = Effect.VisualEffect(VfxType.ImpPulseCold);
        target.ApplyEffect(EffectDuration.Instant, pulseVfx);

        foreach (NwCreature nearby in targetLocation.GetObjectsInShapeByType<NwCreature>(
                     Shape.Sphere, RadiusSize.Medium, true))
        {
            if (nearby == target) continue;
            if (!SpellUtils.IsValidHostileTarget(nearby, casterCreature)) continue;

            int aoeDamage = NWScript.GetReflexAdjustedDamage(damage, nearby, dc, NWScript.SAVING_THROW_TYPE_COLD, casterCreature);
            if (aoeDamage <= 0) continue;

            Effect aoeDamageEffect = Effect.Damage(aoeDamage, DamageType.Cold);
            Effect aoeVfx = Effect.VisualEffect(VfxType.ImpFrostS);

            nearby.ApplyEffect(EffectDuration.Instant, aoeDamageEffect);
            nearby.ApplyEffect(EffectDuration.Instant, aoeVfx);
        }
    }
}
