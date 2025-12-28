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
        int damage = NWScript.d4(numberOfDie);

        Effect damageEffect = Effect.Damage(damage, DamageType.Negative);

        target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
