using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.RayOfHarm;

[ServiceBinding(typeof(RayofHarm))]
public class RayofHarm : ISpell
{
    public string ImpactScript => "am_s_rayofharm";
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;
        
        SpellUtils.SignalSpell(caster, target, eventData.Spell);
        
        ApplyBeam(caster, target);

        int damage = CalculateDamage(caster, casterCreature);

        Effect damageEffect = Effect.Damage(damage, DamageType.Negative);

        ApplyDamage(target, damageEffect);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    private static int CalculateDamage(NwGameObject caster, NwCreature casterCreature)
    {
        int numberOfDie = caster.CasterLevel / 2;

        bool isSpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) ==
                            SpellSchool.Necromancy;
        int damage = isSpecialist ? NWScript.d4(numberOfDie) : NWScript.d3(numberOfDie);
        return damage;
    }

    private static void ApplyBeam(NwGameObject caster, NwGameObject target)
    {
        Effect beam = Effect.Beam(VfxType.BeamBlack, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Instant, beam);
    }

    private void ApplyDamage(NwGameObject target, Effect damageEffect)
    {
        if (ResistedSpell) target.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}