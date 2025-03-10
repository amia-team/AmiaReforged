using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.RayofFrost;

[ServiceBinding(typeof(ISpell))]
public class RayOfFrost : ISpell
{
    public ResistSpellResult Result { get; set; }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
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

        if (Result == ResistSpellResult.Failed) target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }
}