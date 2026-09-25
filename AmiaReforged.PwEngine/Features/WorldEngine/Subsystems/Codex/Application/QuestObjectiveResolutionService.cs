using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Bridges NWN runtime events to the quest objective domain layer.
/// Subscribes to game events (item acquire/lose), translates them into
/// <see cref="QuestSignal"/> instances, routes them through <see cref="QuestSessionManager"/>,
/// and publishes resulting domain events onto the shared <see cref="IEventBus"/>.
/// Also manages session lifecycle: creating sessions on login and quest start,
/// tearing them down on logout and quest completion.
///
/// <para>
/// Resolution is handler-internal plumbing: game code reaches this only via the
/// <c>SetQuestStageCommand</c> handler and the dialogue-entry event handler
/// (F-6 audit — resolution is always downstream of quest advancement).
/// The service never calls command handlers directly; resulting Codex domain events go
/// onto the bus, and the single task-010 forwarding subscriber routes the events that
/// carry a Codex mutation to the processor.
/// </para>
/// </summary>
[ServiceBinding(typeof(QuestObjectiveResolutionService))]
public sealed class QuestObjectiveResolutionService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RuntimeCharacterService _characters;
    private readonly QuestSessionManager _sessionManager;
    private readonly IEventBus _eventBus;
    private readonly IPlayerCodexRepository _codexRepository;

    public QuestObjectiveResolutionService(
        RuntimeCharacterService characters,
        QuestSessionManager sessionManager,
        IEventBus eventBus,
        IPlayerCodexRepository codexRepository)
    {
        _characters = characters;
        _sessionManager = sessionManager;
        _eventBus = eventBus;
        _codexRepository = codexRepository;

        NwModule.Instance.OnAcquireItem += OnAcquireItem;
        NwModule.Instance.OnUnacquireItem += OnUnacquireItem;

        // Subscribe to RuntimeCharacterService events instead of NwModule login events
        // to guarantee the character key is registered before session initialization.
        characters.CharacterReady += OnCharacterReady;
        characters.CharacterLeaving += OnCharacterLeaving;
    }

    private async void OnAcquireItem(ModuleEvents.OnAcquireItem obj)
    {
        try
        {
            NwItem? item = obj.Item;
            if (item is null) return;
            if (!obj.AcquiredBy.IsPlayerControlled(out NwPlayer? player)) return;
            if (player is null) return;
            if (!_characters.TryGetPlayerKey(player, out Guid key) || key == Guid.Empty) return;

            CharacterId characterId = CharacterId.From(key);
            Log.Info("REMOVE LATER: Detected item acquisition: {Item} by character {CharacterId}", item.Tag, characterId);
            await ProcessItemAcquiredAsync(characterId, item.Tag);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process item acquisition for character {CharacterId}", obj.AcquiredBy);
        }
    }

    private async void OnUnacquireItem(ModuleEvents.OnUnacquireItem obj)
    {
        try
        {
            NwItem? item = obj.Item;
            if (item is null) return;

            // OnUnacquireItem fires on the creature that lost the item.
            // The creature reference comes from the module event context.
            NwCreature? creature = obj.LostBy;
            if (creature is null) return;
            if (!creature.IsPlayerControlled(out NwPlayer? player)) return;
            if (player is null) return;
            if (!_characters.TryGetPlayerKey(player, out Guid key) || key == Guid.Empty) return;

            CharacterId characterId = CharacterId.From(key);
            await ProcessItemLostAsync(characterId, item.Tag);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process item loss for character {CharacterId}", obj.LostBy);
        }
    }

    private async void OnCharacterReady(CharacterId characterId)
    {
        try
        {
            await InitializeSessionsForPlayerAsync(characterId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize quest sessions for character {CharacterId}", characterId);
        }
    }

    private void OnCharacterLeaving(CharacterId characterId)
    {
        TeardownSessionsForPlayer(characterId);
    }

    /// <summary>
    /// Processes an item acquisition event for a character.
    /// Translates to a <see cref="SignalType.ItemAcquired"/> signal and routes it
    /// through all active quest sessions, publishing resulting domain events on the bus.
    /// </summary>
    public async Task ProcessItemAcquiredAsync(CharacterId characterId, string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemAcquired, itemTag);
        await RouteSignalAndPublishEventsAsync(characterId, signal);
    }

    /// <summary>
    /// Processes an item loss event for a character.
    /// Translates to a <see cref="SignalType.ItemLost"/> signal and routes it
    /// through all active quest sessions, publishing resulting domain events on the bus.
    /// </summary>
    public async Task ProcessItemLostAsync(CharacterId characterId, string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemLost, itemTag);
        await RouteSignalAndPublishEventsAsync(characterId, signal);
    }

    /// <summary>
    /// Processes a dialogue node entered event for a character.
    /// Translates to a <see cref="SignalType.DialogChoice"/> signal using the
    /// truncated node ID (first 8 hex chars) as the target tag.
    /// This is how "speak to NPC" objectives resolve — the objective definition's
    /// TargetTag is set to the short node ID of the dialogue node that completes it.
    /// </summary>
    public async Task ProcessDialogueNodeEnteredAsync(CharacterId characterId, DialogueNodeId nodeId)
    {
        string shortNodeId = nodeId.ToShortString();
        Log.Info("Processing dialogue node entered: nodeId={NodeId} shortId={ShortId} for character {CharacterId}",
            nodeId.Value, shortNodeId, characterId);
        QuestSignal signal = new(SignalType.DialogChoice, shortNodeId);
        await RouteSignalAndPublishEventsAsync(characterId, signal);
    }

    /// <summary>
    /// Loads the player's codex and creates quest sessions for all active (InProgress)
    /// quests that have objectives defined in their current stage.
    /// Called on login to restore objective tracking.
    /// </summary>
    public async Task InitializeSessionsForPlayerAsync(CharacterId characterId, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _codexRepository.LoadAsync(characterId, ct);
        if (codex == null) return;

        foreach (CodexQuestEntry quest in codex.Quests)
        {
            if (quest.State != QuestState.InProgress) continue;

            CreateSessionForQuest(characterId, quest);
        }
    }

    /// <summary>
    /// Creates a quest session for a single quest entry, using the objectives
    /// from the current stage. If a session already exists for this quest, it is replaced.
    /// </summary>
    public void CreateSessionForQuest(CharacterId characterId, CodexQuestEntry quest)
    {
        // Find the current stage's objective groups
        List<QuestObjectiveGroup> objectiveGroups = GetCurrentStageObjectiveGroups(quest);

        if (objectiveGroups.Count == 0) return;

        // Build stage context so the session can auto-advance when objectives complete
        StageContext? stageContext = quest.Stages.Count > 0
            ? new StageContext(quest.Stages, quest.CurrentStageId)
            : null;

        _sessionManager.CreateSession(characterId, quest.QuestId, objectiveGroups, stageContext: stageContext);

        Log.Info(
            "Created quest session: quest '{QuestId}' stage {StageId} for character {CharacterId} ({GroupCount} objective groups)",
            quest.QuestId.Value, quest.CurrentStageId, characterId, objectiveGroups.Count);
    }

    /// <summary>
    /// Removes all quest sessions for a player. Called on logout.
    /// </summary>
    public void TeardownSessionsForPlayer(CharacterId characterId)
    {
        IReadOnlyCollection<QuestSession> sessions = _sessionManager.GetAllSessions(characterId);
        List<QuestId> questIds = sessions.Select(s => s.QuestId).ToList();

        foreach (QuestId questId in questIds)
        {
            _sessionManager.RemoveSession(characterId, questId);
        }

        if (questIds.Count > 0)
        {
            Log.Info("Tore down {Count} quest sessions for character {CharacterId}",
                questIds.Count, characterId);
        }
    }

    /// <summary>
    /// Routes a signal through the session manager and publishes every resulting
    /// domain event onto the shared bus, once, in the order returned by
    /// <see cref="QuestSessionManager.ProcessSignal"/>.
    /// </summary>
    private async Task RouteSignalAndPublishEventsAsync(
        CharacterId characterId,
        QuestSignal signal,
        CancellationToken ct = default)
    {
        IReadOnlyList<CodexDomainEvent> events = _sessionManager.ProcessSignal(characterId, signal);

        if (events.Count == 0)
        {
            Log.Debug("Signal {SignalType}:{TargetTag} for character {CharacterId} produced no events (no matching objectives or no active sessions)",
                signal.SignalType, signal.TargetTag, characterId);
            return;
        }

        Log.Info("Signal {SignalType}:{TargetTag} for character {CharacterId} produced {EventCount} events",
            signal.SignalType, signal.TargetTag, characterId, events.Count);
        foreach (CodexDomainEvent domainEvent in events)
        {
            await _eventBus.PublishAsync(domainEvent, ct);
        }
    }

    /// <summary>
    /// Extracts the objective groups for the quest entry's current stage.
    /// Returns the objectives from the highest stage ≤ <see cref="CodexQuestEntry.CurrentStageId"/>.
    /// </summary>
    private static List<QuestObjectiveGroup> GetCurrentStageObjectiveGroups(CodexQuestEntry quest)
    {
        if (quest.Stages.Count == 0) return [];

        QuestStage? currentStage = quest.Stages
            .Where(s => s.StageId <= quest.CurrentStageId)
            .OrderByDescending(s => s.StageId)
            .FirstOrDefault();

        return currentStage?.ObjectiveGroups ?? [];
    }
}
