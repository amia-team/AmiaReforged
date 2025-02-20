﻿using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog.Targets;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

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
        
        if(target is not NwCreature creature) return;

        Effect beam = Effect.Beam(VfxType.BeamHoly, caster, BodyNode.Hand);
        target.ApplyEffect(EffectDuration.Instant, beam);
        
        if (NWScript.GetRacialType(target) != NWScript.RACIAL_TYPE_UNDEAD)
        {
            return;
        }

        int numberOfDie = caster.CasterLevel / 2;
        int damage = NWScript.d3(numberOfDie);
        
        Effect damageEffect = Effect.Damage(damage, DamageType.Positive);

        ResistSpellResult result = creature.CheckResistSpell(caster);

        if (result == ResistSpellResult.Failed)
        {
            target.ApplyEffect(EffectDuration.Instant, damageEffect);
        }
    }
}