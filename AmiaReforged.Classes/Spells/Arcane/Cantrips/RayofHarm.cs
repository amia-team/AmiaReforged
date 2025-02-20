using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

[ServiceBinding(typeof(RayofHarm))]
public class RayofHarm : ISpell
{
    public string ImpactScript => "am_s_rayofharm";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if(caster == null) return;
        if(caster is not NwCreature casterCreature) return;
        
        NwGameObject? target = eventData.TargetObject;
        if(target == null) return;
        if(target is not NwCreature creature) return;
        
        Effect beam = Effect.Beam(VfxType.BeamBlack, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Instant, beam);
        
        int numberOfDie = caster.CasterLevel / 2;
        
        bool isSpecialist = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) == SpellSchool.Necromancy;
        int damage = isSpecialist ? NWScript.d4(numberOfDie) : NWScript.d3(numberOfDie);
                
        Effect damageEffect = Effect.Damage(damage, DamageType.Negative);

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