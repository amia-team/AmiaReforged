using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI conversation handler.
/// Ports logic from ds_ai2_convo.nss:
/// - Responds to shout pattern 1001 (M_ATTACKED — ally under attack) by moving toward nearest PC
/// - Handles undead summon follow/stand commands via associate command patterns
/// </summary>
[ServiceBinding(typeof(IOnConversationBehavior))]
public class GenericAiConversation : IOnConversationBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiTargetingService _targetingService;
    private readonly bool _isEnabled;

    /// <summary>
    /// Listen pattern number for the "ally attacked" silent shout.
    /// Matches the legacy constant used in SetListenPattern/GetListenPatternNumber.
    /// </summary>
    private const int ShoutPatternAttacked = 1001;

    public string ScriptName => "ds_ai_convo";

    public GenericAiConversation(
        AiStateManager stateManager,
        AiTargetingService targetingService)
    {
        _stateManager = stateManager;
        _targetingService = targetingService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnConversation(CreatureEvents.OnConversation eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        NwGameObject? speaker = eventData.LastSpeaker;
        int shoutPattern = NWScript.GetListenPatternNumber();

        // --- Shout pattern 1001: ally was attacked (ds_ai2_convo.nss lines 29-41) ---
        if (shoutPattern == ShoutPatternAttacked && !creature.IsInCombat)
        {
            AiState? state = _stateManager.GetState(creature);

            // Try to find a valid target to engage
            NwCreature? nearest = _targetingService.FindNearestEnemy(creature);
            if (nearest != null)
            {
                if (state != null)
                {
                    state.CurrentTarget = nearest;
                    state.MarkActive();
                }

                creature.ClearActionQueue();
                creature.ActionAttackTarget(nearest);
            }
            else
            {
                // No enemy found — move toward nearest PC (legacy fallback)
                NwCreature? nearestPc = creature
                    .GetNearestCreatures()
                    .FirstOrDefault(c => c.IsPlayerControlled);

                if (nearestPc != null)
                {
                    creature.ActionMoveTo(nearestPc, true, 10.0f);
                }
            }

            return;
        }

        // --- Undead summon follow/stand commands (ds_ai2_convo.nss lines 44-59) ---
        if (speaker is NwCreature speakerCreature)
        {
            HandleUndeadSummonCommands(creature, speakerCreature);
        }
    }

    /// <summary>
    /// Handles undead summon follow/stand ground commands.
    /// Ports ds_ai2_convo.nss lines 44-59:
    /// - Creature must have "is_undead" == 1 and speaker must be its master
    /// - FOLLOWMASTER command: follows the speaker
    /// - STANDGROUND command: stops and holds position
    /// </summary>
    private void HandleUndeadSummonCommands(NwCreature creature, NwCreature speaker)
    {
        bool isUndead = creature.GetObjectVariable<LocalVariableInt>("is_undead").Value == 1;
        if (!isUndead) return;

        NwCreature? master = creature.Master;
        if (master != speaker) return;

        int lastCommand = NWScript.GetLastAssociateCommand(creature);

        if (lastCommand == NWScript.ASSOCIATE_COMMAND_FOLLOWMASTER)
        {
            NWScript.AssignCommand(creature, () => NWScript.SpeakString("Hurghhh hhuh..."));
            NWScript.AssignCommand(creature, () => NWScript.ClearAllActions());
            NWScript.AssignCommand(creature, () => NWScript.ActionForceFollowObject(speaker));
        }
        else if (lastCommand == NWScript.ASSOCIATE_COMMAND_STANDGROUND)
        {
            NWScript.AssignCommand(creature, () => NWScript.SpeakString("Buhrhh ghaarg..."));
            NWScript.AssignCommand(creature, () => NWScript.ClearAllActions());
        }
    }
}
