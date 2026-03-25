using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI physical attacked handler.
/// Ports logic from ds_ai2_attacked.nss:
/// - Reputation adjustment (AdjustReputation -100 for PCs, SetIsTemporaryEnemy for confused/dominated)
/// - Broadcasts M_ATTACKED silent shout so nearby allies respond
/// - 25% chance to switch targets mid-fight if already engaged
/// - Wakes inactive creatures and triggers PerformAction
/// </summary>
[ServiceBinding(typeof(IOnPhysicalAttackedBehavior))]
public class GenericAiPhysicalAttacked : IOnPhysicalAttackedBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiTargetingService _targetingService;
    private readonly bool _isEnabled;

    /// <summary>
    /// Silent shout message broadcast when attacked, matching legacy M_ATTACKED constant.
    /// Allies listening on pattern 1001 will respond to this.
    /// </summary>
    private const string AttackedShout = "ds_ai_attacked";

    /// <summary>
    /// Reputation threshold for "friendly" — matches legacy REPUTATION_TYPE_FRIEND.
    /// </summary>
    private const int ReputationFriendly = 11;

    /// <summary>
    /// Chance (out of 100) to switch targets when already fighting someone else.
    /// Legacy: d4 == 1, i.e. 25%.
    /// </summary>
    private const int SwitchTargetChance = 25;

    public string ScriptName => "ds_ai_attacked";

    public GenericAiPhysicalAttacked(
        AiStateManager stateManager,
        AiTargetingService targetingService)
    {
        _stateManager = stateManager;
        _targetingService = targetingService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnPhysicalAttacked(CreatureEvents.OnPhysicalAttacked eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;
        NwCreature? attacker = eventData.Attacker;

        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;
        if (attacker == null) return;

        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        // --- Reputation handling (ds_ai2_attacked.nss lines 33-62) ---
        HandleReputationAdjustment(creature, attacker);

        // --- Combat response ---
        NwGameObject? currentTarget = state.CurrentTarget;
        int inactiveCount = state.InactiveHeartbeats;

        if (inactiveCount > 0)
        {
            // Creature was inactive — wake up and engage attacker
            state.CurrentTarget = attacker;
            state.MarkActive();

            // Try to engage via targeting service
            NwGameObject? validTarget = _targetingService.GetValidTarget(creature, attacker);
            if (validTarget != null)
            {
                creature.ClearActionQueue();
                creature.ActionAttackTarget(validTarget);
            }
            else if (attacker.IsValid)
            {
                // No valid target found but attacker exists - move toward them
                creature.ActionMoveTo(attacker, true, 10.0f);
            }

            // Broadcast silent shout so nearby allies respond (pattern 1001)
            NWScript.SpeakString(AttackedShout, (int)TalkVolume.SilentTalk);
        }
        else if (currentTarget != attacker)
        {
            // Already in combat with a different target — 25% chance to switch
            if (creature.IsCreatureSeen(attacker))
            {
                int roll = Random.Shared.Next(1, 101);
                if (roll <= SwitchTargetChance)
                {
                    state.CurrentTarget = attacker;
                    creature.ClearActionQueue();
                    creature.ActionAttackTarget(attacker);
                }
            }
        }
    }

    /// <summary>
    /// Adjusts reputation based on attacker type and status effects.
    /// Ports ds_ai2_attacked.nss lines 33-62:
    /// - PC/PC-associate attackers: dominated → temporary enemy (10 rounds); otherwise permanent rep loss
    /// - NPC attackers: confused + friendly → temporary enemy; otherwise permanent rep loss
    /// </summary>
    private void HandleReputationAdjustment(NwCreature creature, NwCreature attacker)
    {
        int reputation = NWScript.GetReputation(attacker, creature);
        bool isConfused = attacker.ActiveEffects.Any(e => e.EffectType == EffectType.Confused);
        bool isDominated = attacker.ActiveEffects.Any(e => e.EffectType == EffectType.Dominated);

        bool isPlayerControlled = attacker.IsPlayerControlled ||
                                  (attacker.Master?.IsPlayerControlled ?? false);

        if (isPlayerControlled)
        {
            if (reputation >= ReputationFriendly)
            {
                if (isDominated)
                {
                    // Dominated PC — temporary enemy for 10 rounds (60s)
                    NWScript.SetIsTemporaryEnemy(attacker, creature, 1, 60.0f);
                }
                else
                {
                    // Voluntary attack by PC — permanent reputation loss
                    NWScript.AdjustReputation(attacker, creature, -100);
                }
            }
            else if (!isDominated)
            {
                NWScript.AdjustReputation(attacker, creature, -100);
            }
        }
        else
        {
            // NPC attacker
            if (isConfused && reputation >= ReputationFriendly)
            {
                // Confused ally — temporary enemy for 10 rounds
                NWScript.SetIsTemporaryEnemy(attacker, creature, 1, 60.0f);
            }
            else
            {
                NWScript.AdjustReputation(attacker, creature, -100);
            }
        }
    }
}
