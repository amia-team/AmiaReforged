using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog.Fluent;
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
        
        NWScript.DeleteLocalInt(creature, "AI_BLINDNESS");
        creature.OnCombatRoundStart -= FightSomething;
    }

    private void HandleAiBlindness(OnEffectApply obj)
    {
        if (obj.Object is not NwCreature creature) return;

        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;
        
        if(NWScript.GetLocalInt(creature, "AI_BLINDNESS") == 1) return;
        
        NWScript.SetLocalInt(creature, "AI_BLINDNESS", 1);
        creature.SpeakString("*flails around blindly*");
        creature.OnCombatRoundStart += FightSomething;
    }

    private void FightSomething(OnCombatRoundStart obj)
    {
        NwCreature creature = obj.Creature;

        if (creature.AttackTarget != null)
        {
            creature.SpeakString("DEBUG: I have a target already.");
            return;
        }

        if (creature.IsPlayerControlled || creature.IsDMAvatar)
        {
            creature.SpeakString("DEBUG: I am player controlled.");
            return;
        }

        creature.SpeakString("DEBUG: I'm looking for a target.");
        List<NwCreature> nearbyHostiles = creature.GetNearestObjectsByType<NwCreature>()
            .Where(c => c.IsReactionTypeHostile(creature) && c.Distance(creature) <= 10f).ToList();

        creature.SpeakString($"DEBUG: I metagamed the fuck out of {nearbyHostiles.Count} hostiles nearby.");
        List<NwCreature> hostilesWeCanHear = nearbyHostiles.Where(c => creature.IsCreatureHeard(c)).ToList();
        
        creature.SpeakString($"I can hear {hostilesWeCanHear.Count} hostiles nearby.");

        // Quicksort the list of hostiles by distance to the creature.
        hostilesWeCanHear.Sort((c1, c2) => c1.Distance(creature).CompareTo(c2.Distance(creature)));

        NwCreature? closestHostile = hostilesWeCanHear.FirstOrDefault();


        if (closestHostile is null) return;

        creature.ActionAttackTarget(closestHostile);
    }
}