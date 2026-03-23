using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

/// <summary>
/// Core service managing active dialogue sessions.
/// Orchestrates starting/advancing/ending conversations and bridges domain events.
/// </summary>
[ServiceBinding(typeof(DialogueService))]
public sealed class DialogueService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<NwPlayer, DialogueSession> _activeSessions = new();

    [Inject] private Lazy<IDialogueTreeRepository>? Repository { get; init; }
    [Inject] private Lazy<DialogueConditionRegistry>? ConditionRegistry { get; init; }
    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }
    [Inject] private Lazy<IEventBus>? EventBus { get; init; }
    [Inject] private Lazy<IWorldEngineFacade>? WorldEngine { get; init; }

    /// <summary>
    /// Starts a dialogue conversation between a player and an NPC.
    /// Opens the conversation NUI window.
    /// </summary>
    public async Task<bool> StartDialogueAsync(NwPlayer player, NwCreature npc, DialogueTreeId treeId,
        Guid characterId)
    {
        // End any existing session first
        EndDialogue(player, "new_conversation");

        if (Repository?.Value == null)
        {
            Log.Warn("DialogueTreeRepository not available");
            return false;
        }

        DialogueTree? tree = await Repository.Value.GetByIdAsync(treeId);

        // DB call above completes on a thread-pool thread — switch back to the main
        // NWN server thread before touching any game objects.
        await NwTask.SwitchToMainThread();

        if (tree == null)
        {
            Log.Warn("Dialogue tree '{TreeId}' not found", treeId.Value);
            player.SendServerMessage($"Dialogue not found: {treeId.Value}", ColorConstants.Orange);
            return false;
        }

        List<string> errors = tree.Validate();
        if (errors.Count > 0)
        {
            Log.Warn("Dialogue tree '{TreeId}' failed validation: {Errors}",
                treeId.Value, string.Join("; ", errors));
            player.SendServerMessage("This dialogue has configuration errors.", ColorConstants.Orange);
            return false;
        }

        DialogueSession session = new(tree, player, characterId, npc);
        _activeSessions[player] = session;

        // Fire node-enter actions for the root node
        await ExecuteNodeActions(session);

        // Open the NUI window
        OpenConversationWindow(session);

        // Publish event
        await PublishEventAsync(new DialogueStartedEvent
        {
            DialogueTreeId = treeId,
            CharacterId = characterId,
            NpcTag = npc.Tag
        });

        Log.Info("Started dialogue '{TreeId}' between {Player} and {Npc}",
            treeId.Value, player.PlayerName, npc.Name);

        return true;
    }

    /// <summary>
    /// Advances the dialogue by selecting a choice at the given index from the visible choices.
    /// </summary>
    public async Task<bool> AdvanceDialogueAsync(NwPlayer player, int choiceIndex)
    {
        if (!_activeSessions.TryGetValue(player, out DialogueSession? session))
        {
            Log.Warn("No active dialogue session for {Player}", player.PlayerName);
            return false;
        }

        if (session.IsEnded) return false;

        if (ConditionRegistry?.Value == null)
        {
            Log.Warn("DialogueConditionRegistry not available");
            return false;
        }

        // Get visible choices
        List<DialogueChoice> visibleChoices =
            await session.GetVisibleChoicesAsync(ConditionRegistry.Value);

        // Condition evaluation may involve async work — ensure we're back on the main thread.
        await NwTask.SwitchToMainThread();

        if (choiceIndex < 0 || choiceIndex >= visibleChoices.Count)
        {
            Log.Warn("Invalid choice index {Index} (available: {Count})", choiceIndex, visibleChoices.Count);
            return false;
        }

        DialogueChoice choice = visibleChoices[choiceIndex];
        DialogueNode? fromNode = session.GetCurrentNode();
        DialogueNodeId fromNodeId = session.CurrentNodeId;

        // Advance to the target node
        DialogueNode? newNode = session.SelectChoice(choice);
        if (newNode == null)
        {
            Log.Warn("Choice target node not found");
            return false;
        }

        // Publish choice event (for quest signal integration)
        await PublishEventAsync(new DialogueChoiceMadeEvent
        {
            DialogueTreeId = session.Tree.Id,
            FromNodeId = fromNodeId,
            ToNodeId = choice.TargetNodeId,
            ChoiceIndex = choiceIndex,
            ChoiceText = choice.ResponseText,
            CharacterId = session.CharacterId
        });

        // Execute the new node's actions
        await ExecuteNodeActions(session);
        await NwTask.SwitchToMainThread();

        // Publish node entered event
        await PublishEventAsync(new DialogueNodeEnteredEvent
        {
            DialogueTreeId = session.Tree.Id,
            NodeId = choice.TargetNodeId,
            CharacterId = session.CharacterId
        });

        // If the new node is an Action type with no text, auto-advance through it
        while (newNode != null && newNode.Type == DialogueNodeType.Action && newNode.Choices.Count == 1)
        {
            DialogueChoice autoChoice = newNode.Choices[0];
            newNode = session.SelectChoice(autoChoice);
            if (newNode != null)
            {
                await ExecuteNodeActions(session);
                await NwTask.SwitchToMainThread();
            }
        }

        // If we reached an End node, close the conversation
        if (session.IsEnded)
        {
            EndDialogue(player, "end_node");
            return true;
        }

        return true;
    }

    /// <summary>
    /// Ends the active dialogue for a player.
    /// </summary>
    public void EndDialogue(NwPlayer player, string reason = "goodbye")
    {
        if (!_activeSessions.TryGetValue(player, out DialogueSession? session))
            return;

        session.End();
        _activeSessions.Remove(player);

        // Close the NUI window
        if (WindowDirector?.Value != null)
        {
            WindowDirector.Value.CloseWindow(player, typeof(ConversationPresenter));
        }

        // Publish event (fire and forget)
        _ = PublishEventAsync(new DialogueEndedEvent
        {
            DialogueTreeId = session.Tree.Id,
            CharacterId = session.CharacterId,
            Reason = reason
        });

        Log.Info("Ended dialogue '{TreeId}' for {Player} (reason: {Reason})",
            session.Tree.Id.Value, player.PlayerName, reason);
    }

    /// <summary>
    /// Gets the active dialogue session for a player, if any.
    /// </summary>
    public DialogueSession? GetActiveSession(NwPlayer player) =>
        _activeSessions.TryGetValue(player, out DialogueSession? session) ? session : null;

    /// <summary>
    /// Checks if a player has an active dialogue session.
    /// </summary>
    public bool HasActiveSession(NwPlayer player) => _activeSessions.ContainsKey(player);

    /// <summary>
    /// Gets all dialogue trees available for an NPC by its speaker tag.
    /// </summary>
    public async Task<List<DialogueTree>> GetDialoguesForNpcAsync(string npcTag)
    {
        if (Repository?.Value == null) return [];
        return await Repository.Value.GetBySpeakerTagAsync(npcTag);
    }

    // ──────────────────── Internal ────────────────────

    private async Task ExecuteNodeActions(DialogueSession session)
    {
        DialogueNode? node = session.GetCurrentNode();
        if (node == null || node.Actions.Count == 0) return;

        if (WorldEngine?.Value == null)
        {
            Log.Warn("WorldEngineFacade not available for action execution");
            return;
        }

        foreach (DialogueAction action in node.Actions.OrderBy(a => a.ExecutionOrder))
        {
            ExecuteDialogueActionCommand command = new()
            {
                Action = action,
                Player = session.Player,
                CharacterId = session.CharacterId,
                Npc = session.Npc
            };

            CommandResult result = await WorldEngine.Value.ExecuteAsync(command);

            // Command handlers may do async DB/network work — get back on the main thread.
            await NwTask.SwitchToMainThread();

            if (!result.Success)
            {
                Log.Warn("Dialogue action {ActionType} failed: {Error}", action.ActionType, result.ErrorMessage);
            }
        }
    }

    private void OpenConversationWindow(DialogueSession session)
    {
        if (WindowDirector?.Value == null)
        {
            Log.Warn("WindowDirector not available for opening conversation window");
            return;
        }

        ConversationView view = new(session.Player, this);
        IScryPresenter presenter = view.Presenter;

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(presenter);

        WindowDirector.Value.OpenWindow(presenter);
    }

    private async Task PublishEventAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        if (EventBus?.Value == null) return;

        try
        {
            await EventBus.Value.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Error publishing dialogue event {EventType}", typeof(TEvent).Name);
        }
    }
}
