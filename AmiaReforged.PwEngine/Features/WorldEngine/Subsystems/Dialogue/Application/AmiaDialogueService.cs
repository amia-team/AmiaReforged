using AmiaReforged.Shared.Dialogue;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
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
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

[ServiceBinding(typeof(AmiaDialogueService))]
public sealed class AmiaDialogueService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<NwPlayer, DialogueSession> _activeSessions = new();
    private readonly Dictionary<NwCreature, NwPlayer> _busyNpcs = new();
    private readonly HashSet<NwPlayer> _startingPlayers = [];
    private readonly HashSet<NwCreature> _startingNpcs = [];

    [Inject] private Lazy<IDialogueTreeRepository>? Repository { get; init; }
    [Inject] private Lazy<DialogueConditionRegistry>? ConditionRegistry { get; init; }
    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }
    [Inject] private Lazy<IEventBus>? EventBus { get; init; }
    [Inject] private Lazy<IWorldEngineFacade>? WorldEngine { get; init; }

    public AmiaDialogueService()
    {
        NwModule.Instance.OnClientLeave += OnClientLeave;
        NwModule.Instance.OnHeartbeat += _ =>
        {
            foreach (DialogueSession session in _activeSessions.Values.ToList())
                if (!session.Npc.IsValid || session.Player.LoginCreature is null) EndDialogue(session.Player, "participant_removed");
        };
    }

    private void OnClientLeave(ModuleEvents.OnClientLeave evt)
    {
        _startingPlayers.Remove(evt.Player);
        EndDialogue(evt.Player, "disconnected");
    }

    public async Task<bool> StartDialogueAsync(NwPlayer player, NwCreature npc, DialogueTreeId treeId, Guid characterId)
    {
        await NwTask.SwitchToMainThread();
        if (_startingPlayers.Contains(player) || IsNpcBusy(npc) || !npc.IsValid || player.LoginCreature == null) return false;
        EndDialogue(player, "new_conversation");
        _startingPlayers.Add(player);
        _startingNpcs.Add(npc);
        try
        {
            if (Repository?.Value is null || ConditionRegistry?.Value is null) return false;
            // Each new conversation loads the latest committed definition. Active sessions
            // keep this snapshot so a save cannot replay actions/rewards already executed.
            DialogueTree? tree = await Repository.Value.GetByIdAsync(treeId);
            await NwTask.SwitchToMainThread();
            if (!_startingPlayers.Contains(player) || !npc.IsValid || player.LoginCreature == null) return false;
            if (tree is null)
            {
                player.SendServerMessage($"Dialogue not found: {treeId.Value}", ColorConstants.Orange);
                return false;
            }
            List<string> errors = tree.Validate();
            if (errors.Count > 0) throw new FormatException(string.Join("; ", errors));

            DialogueSession session = new(tree, player, characterId, npc);
            _activeSessions[player] = session;
            _busyNpcs[npc] = player;
            await PublishEventAsync(new DialogueStartedEvent { DialogueTreeId = treeId, CharacterId = characterId, NpcTag = npc.Tag });
            DialoguePlaybackResult result = await session.Playback.StartAsync(
                conditions => session.EvaluateAsync(ConditionRegistry.Value, conditions),
                node => EnterNodeAsync(session, node));
            await NwTask.SwitchToMainThread();
            if (!result.Success)
            {
                player.SendServerMessage(result.Error ?? "Unable to start conversation.", ColorConstants.Orange);
                EndDialogue(player, "start_failed");
                return false;
            }
            if (!IsCurrent(session)) return false;
            if (session.Playback.IsTerminal && string.IsNullOrWhiteSpace(session.GetCurrentNode()?.Text))
                EndDialogue(player, "end_node");
            else if (!OpenConversationWindow(session))
            {
                EndDialogue(player, "window_failed");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            Log.Error(ex, "Failed to start dialogue '{TreeId}'", treeId.Value);
            if (player.LoginCreature != null) player.SendServerMessage("This dialogue could not be started. Please contact a DM.", ColorConstants.Orange);
            EndDialogue(player, "start_failed");
            return false;
        }
        finally
        {
            await NwTask.SwitchToMainThread();
            _startingPlayers.Remove(player);
            _startingNpcs.Remove(npc);
        }
    }

    // The UI sends the identity of the reply it displayed, not its position in a
    // reevaluated list. Conditions changing cannot redirect the click to another reply.
    public async Task<bool> AdvanceDialogueAsync(NwPlayer player, DialogueNodeId fromNodeId, Guid choiceId)
    {
        await NwTask.SwitchToMainThread();
        DialogueSession? session = GetActiveSession(player);
        if (session is null || session.IsEnded || session.Playback.IsBusy || ConditionRegistry?.Value is null) return false;
        DialogueNode? fromNode = session.Tree.FindNode(fromNodeId);
        DialogueChoice? choice = fromNode?.Choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice is null) return false;
        bool entered = false;
        try
        {
            DialoguePlaybackResult result = await session.Playback.ChooseAsync(fromNodeId.Value.ToString(), choiceId.ToString(),
                conditions => session.EvaluateAsync(ConditionRegistry.Value, conditions), async node =>
                {
                    await NwTask.SwitchToMainThread();
                    if (!IsCurrent(session)) return false;
                    if (!entered)
                    {
                        entered = true;
                        if (!choice.IsContinue) SpeakChoiceText(session, choice);
                        await PublishEventAsync(new DialogueChoiceMadeEvent
                        {
                            DialogueTreeId = session.Tree.Id, FromNodeId = fromNodeId, ToNodeId = choice.TargetNodeId,
                            ChoiceIndex = fromNode!.Choices.IndexOf(choice), ChoiceText = choice.ResponseText, CharacterId = session.CharacterId
                        });
                    }
                    return await EnterNodeAsync(session, node);
                });
            await NwTask.SwitchToMainThread();
            if (!result.Success)
            {
                if (IsCurrent(session))
                {
                    player.SendServerMessage(result.Error ?? "Unable to advance conversation.", ColorConstants.Orange);
                    // Effects may have partially completed; end instead of offering a retry
                    // that would execute earlier effects again.
                    if (entered) EndDialogue(player, "action_failed");
                }
                return false;
            }
            if (IsCurrent(session) && session.Playback.IsTerminal && string.IsNullOrWhiteSpace(session.GetCurrentNode()?.Text))
                EndDialogue(player, "end_node");
            return true;
        }
        catch (Exception ex)
        {
            await NwTask.SwitchToMainThread();
            Log.Error(ex, "Failed to advance dialogue '{TreeId}'", session.Tree.Id.Value);
            if (IsCurrent(session))
            {
                player.SendServerMessage("The conversation could not continue. Please contact a DM.", ColorConstants.Orange);
                EndDialogue(player, "advance_failed");
            }
            return false;
        }
    }

    private bool IsCurrent(DialogueSession session) => !session.IsEnded && ReferenceEquals(GetActiveSession(session.Player), session);

    private async Task<bool> EnterNodeAsync(DialogueSession session, DialogueNodeDto definition)
    {
        await NwTask.SwitchToMainThread();
        if (!IsCurrent(session) || !session.Npc.IsValid || session.Player.LoginCreature == null) return false;
        DialogueNode? node = session.GetCurrentNode();
        if (node is null) return false;
        session.TextPage = 0;
        if (node.Type != DialogueNodeType.Action && !string.IsNullOrWhiteSpace(node.Text) && session.GetCurrentSpeaker() is null)
        {
            Log.Warn("Speaker '{Tag}' not found in the conversation area", node.SpeakerTag);
            return false;
        }
        foreach (DialogueAction action in node.Actions.OrderBy(a => a.ExecutionOrder))
        {
            if (!IsCurrent(session) || WorldEngine?.Value is null) return false;
            CommandResult result = await WorldEngine.Value.ExecuteAsync(new ExecuteDialogueActionCommand
            {
                Action = action, Player = session.Player, CharacterId = session.CharacterId, Npc = session.Npc
            });
            await NwTask.SwitchToMainThread();
            if (!result.Success)
            {
                Log.Warn("Dialogue action {Type} failed: {Error}", action.ActionType, result.ErrorMessage);
                if (IsCurrent(session)) session.Player.SendServerMessage($"Conversation action failed: {result.ErrorMessage}", ColorConstants.Orange);
                return false;
            }
        }
        if (!IsCurrent(session)) return false;
        await PublishEventAsync(new DialogueNodeEnteredEvent { DialogueTreeId = session.Tree.Id, NodeId = node.Id, CharacterId = session.CharacterId });
        await NwTask.SwitchToMainThread();
        if (!IsCurrent(session)) return false;
        if (node.Type != DialogueNodeType.Action && !string.IsNullOrWhiteSpace(node.Text)) session.GetCurrentSpeaker()?.SpeakString(node.Text);
        return true;
    }

    public void EndDialogue(NwPlayer player, string reason = "goodbye", bool closeWindow = true)
    {
        if (!_activeSessions.Remove(player, out DialogueSession? session)) return;
        session.End();
        _busyNpcs.Remove(session.Npc);
        if (closeWindow) WindowDirector?.Value.CloseWindow(player, typeof(ConversationPresenter));
        _ = PublishEventAsync(new DialogueEndedEvent { DialogueTreeId = session.Tree.Id, CharacterId = session.CharacterId, Reason = reason });
    }

    public DialogueSession? GetActiveSession(NwPlayer player) => _activeSessions.GetValueOrDefault(player);
    public bool HasActiveSession(NwPlayer player) => _activeSessions.ContainsKey(player) || _startingPlayers.Contains(player);
    public bool IsNpcBusy(NwCreature npc) => _busyNpcs.ContainsKey(npc) || _startingNpcs.Contains(npc);
    public NwPlayer? GetPlayerTalkingTo(NwCreature npc) => _busyNpcs.GetValueOrDefault(npc);
    public async Task<List<DialogueTree>> GetDialoguesForNpcAsync(string npcTag) => Repository?.Value is { } repository ? await repository.GetBySpeakerTagAsync(npcTag) : [];

    private bool OpenConversationWindow(DialogueSession session)
    {
        if (WindowDirector?.Value is null) return false;
        ConversationView view = new(session.Player, this);
        IScryPresenter presenter = view.Presenter;
        AnvilCore.GetService<InjectionService>()!.Inject(presenter);
        WindowDirector.Value.OpenWindow(presenter);
        return true;
    }

    private async Task PublishEventAsync<TEvent>(TEvent evt) where TEvent : IDomainEvent
    {
        if (EventBus?.Value is null) return;
        try { await EventBus.Value.PublishAsync(evt); }
        catch (Exception ex) { Log.Warn(ex, "Error publishing dialogue event {EventType}", typeof(TEvent).Name); }
    }

    private static void SpeakChoiceText(DialogueSession session, DialogueChoice choice)
    {
        if (!string.IsNullOrWhiteSpace(choice.ResponseText)) session.Player.LoginCreature?.SpeakString(choice.ResponseText);
    }
}
