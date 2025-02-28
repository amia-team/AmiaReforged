using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.DisruptUndead;

[ServiceBinding(typeof(ISpell))]
public class DisruptUndead : ISpell
{
    public string ImpactScript => "am_s_disruptun";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if(caster == null) return;
        
        NwGameObject? target = eventData.TargetObject;
        if(target == null) return;
        
        if(caster is not NwCreature casterCreature) return;

        ApplyBeam(caster, target);

        if (NWScript.GetRacialType(target) != NWScript.RACIAL_TYPE_UNDEAD) return;

        ApplyDamage(caster, casterCreature, target);
    }

    private static void ApplyBeam(NwGameObject caster, NwGameObject target)
    {
        Effect beam = Effect.Beam(VfxType.BeamHoly, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Instant, beam);
    }

    private void ApplyDamage(NwGameObject caster, NwCreature casterCreature, NwGameObject target)
    {
        int numberOfDie = caster.CasterLevel / 2;
        int damage = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) == SpellSchool.Necromancy ? NWScript.d4(numberOfDie) : NWScript.d3(numberOfDie);
        
        Effect damageEffect = Effect.Damage(damage, DamageType.Positive);
        
        if (Result == ResistSpellResult.Failed)
        {
            target.ApplyEffect(EffectDuration.Instant, damageEffect);
        }
    }

    public ResistSpellResult Result { get; set; }
    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void SetResult(ResistSpellResult result)
    {
        Result = result;
    }
}