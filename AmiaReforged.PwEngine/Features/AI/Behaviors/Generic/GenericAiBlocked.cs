using AmiaReforged.PwEngine.Features.AI.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI blocked handler that manages door bashing and blocking behavior.
/// Ports logic from ds_ai_blocked.nss.
/// </summary>
[ServiceBinding(typeof(IOnBlockedBehavior))]
public class GenericAiBlocked : IOnBlockedBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_blocked";

    public GenericAiBlocked(AiStateManager stateManager)
    {
        _stateManager = stateManager;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnBlocked(CreatureEvents.OnBlocked eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // Get the blocking object via NWScript
        uint blockerObjectId = NWScript.GetBlockingDoor();
        NwGameObject? blocker = blockerObjectId.ToNwObject<NwGameObject>();

        if (blocker == null) return;

        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        // Handle door blocking
        if (blocker is NwDoor door)
        {
            // Try to bash down the door if it's not locked or plot-flagged
            if (!door.Locked && !door.PlotFlag)
            {
                creature.ActionAttackTarget(door);
            }
            else
            {
                // Can't break through, clear target and retreat
                state.CurrentTarget = null;
                creature.ClearActionQueue();
            }
        }
        // Handle creature blocking
        else if (blocker is NwCreature blockerCreature)
        {
            // Track the blocking creature for pathfinding
            state.BlockedBy = blockerCreature;

            // If the blocker is an enemy, attack it
            if (blockerCreature.IsEnemy(creature))
            {
                state.CurrentTarget = blocker;
            }
        }
    }
}
