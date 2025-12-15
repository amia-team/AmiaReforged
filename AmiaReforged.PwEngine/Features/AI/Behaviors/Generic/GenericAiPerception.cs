using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI perception handler that wakes creatures from sleep and tracks perceived enemies.
/// Ports logic from ds_ai_perceive.nss.
/// </summary>
[ServiceBinding(typeof(IOnPerceptionBehavior))]
public class GenericAiPerception : IOnPerceptionBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_perceive";

    public GenericAiPerception(AiStateManager stateManager)
    {
        _stateManager = stateManager;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnPerception(CreatureEvents.OnPerception eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        NwCreature? perceived = eventData.PerceivedCreature;
        if (perceived == null) return;

        // Only react to enemies
        if (!perceived.IsEnemy(creature)) return;

        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        // Wake creature from sleep mode by marking as active
        state.MarkActive();

        // Set perception flag for this perceived creature
        // This is used by IsDetectable() in AiTargetingService
        string perceptionVar = $"ds_ai_p{perceived.Name.Substring(0, Math.Min(12, perceived.Name.Length))}";
        creature.GetObjectVariable<LocalVariableInt>(perceptionVar).Value = 1;
    }
}

