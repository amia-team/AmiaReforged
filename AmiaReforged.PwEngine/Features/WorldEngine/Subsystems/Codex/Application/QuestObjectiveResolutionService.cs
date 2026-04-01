using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Bridges NWN runtime events to the quest objective domain layer.
/// Subscribes to game events (item acquire/lose), translates them into
/// <see cref="QuestSignal"/> instances, routes them through <see cref="QuestSessionManager"/>,
/// and enqueues resulting domain events into <see cref="CodexEventProcessor"/>.
/// Also manages session lifecycle: creating sessions on login and quest start,
/// tearing them down on logout and quest completion.
/// </summary>
[ServiceBinding(typeof(QuestObjectiveResolutionService))]
public sealed class QuestObjectiveResolutionService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly RuntimeCharacterService _characters;
    private readonly QuestSessionManager _sessionManager;
    private readonly CodexEventProcessor _eventProcessor;
    private readonly IPlayerCodexRepository _codexRepository;

    public QuestObjectiveResolutionService(
        RuntimeCharacterService characters,
        QuestSessionManager sessionManager,
        CodexEventProcessor eventProcessor,
        IPlayerCodexRepository codexRepository)
    {
        _characters = characters;
        _sessionManager = sessionManager;
        _eventProcessor = eventProcessor;
        _codexRepository = codexRepository;

        NwModule.Instance.OnAcquireItem += OnAcquireItem;
        NwModule.Instance.OnUnacquireItem += OnUnacquireItem;
        NwModule.Instance.OnClientEnter += OnClientEnter;
        NwModule.Instance.OnClientLeave += OnClientLeave;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NWN Event Handlers (thin adapters → testable methods)
    // ═══════════════════════════════════════════════════════════════════

    private void OnAcquireItem(ModuleEvents.OnAcquireItem obj)
    {
        NwItem? item = obj.Item;
        if (item is null) return;
        if (!obj.AcquiredBy.IsPlayerControlled(out NwPlayer? player)) return;
        if (player is null) return;
        if (!_characters.TryGetPlayerKey(player, out Guid key) || key == Guid.Empty) return;

        CharacterId characterId = CharacterId.From(key);
        ProcessItemAcquired(characterId, item.Tag);
    }

    private void OnUnacquireItem(ModuleEvents.OnUnacquireItem obj)
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
        ProcessItemLost(characterId, item.Tag);
    }

    private async void OnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        Guid key = PcKeyUtils.GetPcKey(obj.Player);
        if (key == Guid.Empty) return;

        CharacterId characterId = CharacterId.From(key);

        try
        {
            await InitializeSessionsForPlayerAsync(characterId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize quest sessions for character {CharacterId}", characterId);
        }
    }

    private void OnClientLeave(ModuleEvents.OnClientLeave obj)
    {
        if (obj.Player.IsDM) return;
        if (!_characters.TryGetPlayerKey(obj.Player, out Guid key) || key == Guid.Empty) return;

        CharacterId characterId = CharacterId.From(key);
        TeardownSessionsForPlayer(characterId);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Public testable methods — signal processing
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Processes an item acquisition event for a character.
    /// Translates to a <see cref="SignalType.ItemAcquired"/> signal and routes it
    /// through all active quest sessions.
    /// </summary>
    public void ProcessItemAcquired(CharacterId characterId, string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemAcquired, itemTag);
        RouteSignalAndEnqueueEvents(characterId, signal);
    }

    /// <summary>
    /// Processes an item loss event for a character.
    /// Translates to a <see cref="SignalType.ItemLost"/> signal and routes it
    /// through all active quest sessions.
    /// </summary>
    public void ProcessItemLost(CharacterId characterId, string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemLost, itemTag);
        RouteSignalAndEnqueueEvents(characterId, signal);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Session lifecycle
    // ═══════════════════════════════════════════════════════════════════

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

        _sessionManager.CreateSession(characterId, quest.QuestId, objectiveGroups);

        Log.Debug("Created quest session: quest '{QuestId}' stage {StageId} for character {CharacterId} ({GroupCount} objective groups)",
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
            Log.Debug("Tore down {Count} quest sessions for character {CharacterId}",
                questIds.Count, characterId);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════════════

    private void RouteSignalAndEnqueueEvents(CharacterId characterId, QuestSignal signal)
    {
        IReadOnlyList<CodexDomainEvent> events = _sessionManager.ProcessSignal(characterId, signal);

        if (events.Count == 0) return;

        foreach (CodexDomainEvent domainEvent in events)
        {
            // Fire-and-forget enqueue; the event processor handles persistence asynchronously
            _ = _eventProcessor.EnqueueEventAsync(domainEvent);
        }

        Log.Debug("Signal {SignalType}:{TargetTag} produced {EventCount} events for character {CharacterId}",
            signal.SignalType, signal.TargetTag, events.Count, characterId);
    }

    /// <summary>
    /// Extracts the objective groups for the quest entry's current stage.
    /// Returns the objectives from the highest stage ≤ <see cref="CodexQuestEntry.CurrentStageId"/>.
    /// </summary>
    internal static List<QuestObjectiveGroup> GetCurrentStageObjectiveGroups(CodexQuestEntry quest)
    {
        if (quest.Stages.Count == 0) return [];

        QuestStage? currentStage = quest.Stages
            .Where(s => s.StageId <= quest.CurrentStageId)
            .OrderByDescending(s => s.StageId)
            .FirstOrDefault();

        return currentStage?.ObjectiveGroups ?? [];
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Static test-friendly methods (no NWN dependencies)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Static entry point for tests — processes an item acquired signal
    /// without requiring NWN module references.
    /// </summary>
    internal static void ProcessItemAcquiredStatic(
        QuestSessionManager sessionManager,
        System.Threading.Channels.Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemAcquired, itemTag);
        RouteSignalStatic(sessionManager, eventChannel, characterId, signal);
    }

    /// <summary>
    /// Static entry point for tests — processes an item lost signal
    /// without requiring NWN module references.
    /// </summary>
    internal static void ProcessItemLostStatic(
        QuestSessionManager sessionManager,
        System.Threading.Channels.Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        string itemTag)
    {
        QuestSignal signal = new(SignalType.ItemLost, itemTag);
        RouteSignalStatic(sessionManager, eventChannel, characterId, signal);
    }

    /// <summary>
    /// Static entry point for tests — creates a quest session from a quest entry
    /// without requiring NWN module references.
    /// </summary>
    internal static void CreateSessionForQuestStatic(
        QuestSessionManager sessionManager,
        CharacterId characterId,
        CodexQuestEntry quest)
    {
        List<QuestObjectiveGroup> objectiveGroups = GetCurrentStageObjectiveGroups(quest);
        if (objectiveGroups.Count == 0) return;
        sessionManager.CreateSession(characterId, quest.QuestId, objectiveGroups);
    }

    private static void RouteSignalStatic(
        QuestSessionManager sessionManager,
        System.Threading.Channels.Channel<CodexDomainEvent> eventChannel,
        CharacterId characterId,
        QuestSignal signal)
    {
        IReadOnlyList<CodexDomainEvent> events = sessionManager.ProcessSignal(characterId, signal);
        if (events.Count == 0) return;

        foreach (CodexDomainEvent domainEvent in events)
        {
            eventChannel.Writer.TryWrite(domainEvent);
        }
    }
}
