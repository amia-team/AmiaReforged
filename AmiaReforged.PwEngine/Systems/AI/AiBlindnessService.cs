﻿using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.AI;

[ServiceBinding(typeof(AiBlindnessService))]
public class AiBlindnessService
{
    public AiBlindnessService()
    {
        NwModule.Instance.OnEffectApply += HandleAiBlindness;
        NwModule.Instance.OnEffectRemove += ReturnToNormal;
    }

    private void ReturnToNormal(OnEffectRemove obj)
    {
        if (obj.Object is not NwCreature creature) return;

        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;
        creature.OnCombatRoundStart -= FightSomething;
    }

    private void HandleAiBlindness(OnEffectApply obj)
    {
        if (obj.Object is not NwCreature creature) return;

        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;
        
        creature.OnCombatRoundStart += FightSomething;
    }

    private void FightSomething(OnCombatRoundStart obj)
    {
        NwCreature creature = obj.Creature;
        
        if(creature.AttackTarget != null) return;
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;
        creature.OnCombatRoundStart += FightSomething;

        List<NwCreature> nearbyHostiles = creature.GetNearestObjectsByType<NwCreature>()
            .Where(c => c.IsReactionTypeHostile(creature) && c.Distance(creature) <= 10f).ToList();

        List<NwCreature> hostilesWeCanHear = nearbyHostiles.Where(c => creature.IsCreatureHeard(c)).ToList();

        // Quicksort the list of hostiles by distance to the creature.
        hostilesWeCanHear.Sort((c1, c2) => c1.Distance(creature).CompareTo(c2.Distance(creature)));

        NwCreature? closestHostile = hostilesWeCanHear.FirstOrDefault();

        if (closestHostile is null) return;

        creature.ActionAttackTarget(closestHostile);
    }
}