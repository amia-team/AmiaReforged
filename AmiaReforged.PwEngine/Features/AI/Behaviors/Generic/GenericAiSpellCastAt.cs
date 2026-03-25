using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI spell cast at handler.
/// Ports logic from ds_ai2_spellcast.nss:
/// - Wakes inactive creatures when hit by a hostile spell
/// - Triggers PerformAction to engage the caster
/// </summary>
[ServiceBinding(typeof(IOnSpellCastAtBehavior))]
public class GenericAiSpellCastAt : IOnSpellCastAtBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiTargetingService _targetingService;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_spellcast";

    public GenericAiSpellCastAt(
        AiStateManager stateManager,
        AiTargetingService targetingService)
    {
        _stateManager = stateManager;
        _targetingService = targetingService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnSpellCastAt(CreatureEvents.OnSpellCastAt eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;
        NwGameObject? caster = eventData.Caster;

        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;
        if (caster == null) return;

        // Only respond to hostile spells from enemies
        if (!eventData.Harmful) return;
        if (caster is NwCreature casterCreature && !casterCreature.IsEnemy(creature)) return;

        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        int inactiveCount = state.InactiveHeartbeats;

        // Legacy logic: respond if inactive count is 0 or > 1
        // (count == 0 means just spawned, > 1 means idle — both should react)
        // count == 1 means already active from another event
        if (inactiveCount == 0 || inactiveCount > 1)
        {
            // Wake up and engage
            state.MarkActive();
            state.CurrentTarget = caster;

            NwGameObject? target = _targetingService.GetValidTarget(creature, caster);
            if (target != null)
            {
                creature.ClearActionQueue();
                creature.ActionAttackTarget(target);
            }
        }
    }
}
