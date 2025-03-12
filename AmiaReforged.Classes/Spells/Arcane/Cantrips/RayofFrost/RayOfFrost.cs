using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.RayofFrost;

[ServiceBinding(typeof(ISpell))]
public class RayOfFrost : ISpell
{
    public bool ResistedSpell { get; set; }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster);
    }

    public string ImpactScript => "NW_S0_RayFrost";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        Effect beam = Effect.Beam(VfxType.BeamCold, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Instant, beam);

        int numberOfDie = caster.CasterLevel / 2;
        int damage = NWScript.d3(numberOfDie);

        Effect damageEffect = Effect.Damage(damage, DamageType.Cold);

        if (!ResistedSpell) target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}