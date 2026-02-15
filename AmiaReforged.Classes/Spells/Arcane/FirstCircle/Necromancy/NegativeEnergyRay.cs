using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Necromancy;

[ServiceBinding(typeof(ISpell))]
public class NegativeEnergyRay : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_NegRay";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        Effect beam = Effect.Beam(VfxType.BeamCold, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Temporary, beam, TimeSpan.FromSeconds(1.1));

        SpellUtils.SignalSpell(caster, target, eventData.Spell);

        if (ResistedSpell) return;

        int numberOfDie = caster.CasterLevel / 2;
        int amount = NWScript.d4(numberOfDie);

        bool isUndead = NWScript.GetRacialType(target) == NWScript.RACIAL_TYPE_UNDEAD;

        Effect effect = isUndead
            ? Effect.Heal(amount)
            : Effect.Damage(amount, DamageType.Negative);

        Effect vfx = isUndead
            ? Effect.VisualEffect(VfxType.ImpHealingS)
            : Effect.VisualEffect(VfxType.ImpNegativeEnergy);

        target.ApplyEffect(EffectDuration.Instant, effect);
        target.ApplyEffect(EffectDuration.Instant, vfx);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
