using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;

/// <summary>
/// Optional context that enables automatic stage advancement when objective groups complete.
/// When provided, the session can resolve the next stage using the priority chain:
/// <c>Group.CompletionStageId > Stage.NextStageId > next numeric stage ID</c>.
/// </summary>
/// <param name="AllStages">All stages defined for this quest, in any order.</param>
/// <param name="CurrentStageId">The stage the player is currently on.</param>
public sealed record StageContext(IReadOnlyList<QuestStage> AllStages, int CurrentStageId);

/// <summary>
/// Runtime aggregate managing the live state of a player's active quest objectives.
/// Holds resolved evaluators and mutable objective state, processes incoming signals,
/// and produces domain events describing what changed.
/// </summary>
public class QuestSession
{
    private readonly IObjectiveEvaluatorRegistry _evaluatorRegistry;
    private readonly Dictionary<ObjectiveId, ObjectiveState> _objectiveStates = new();
    private readonly Dictionary<ObjectiveId, ObjectiveDefinition> _objectiveDefinitions = new();
    private List<QuestObjectiveGroup> _groups;
    private readonly Dictionary<int, bool> _groupCompletionStatus = new();

    // Stage context for auto-advancement (null = no auto-advancement; backward compatible)
    private readonly IReadOnlyList<QuestStage>? _allStages;
    private int _currentStageId;

    /// <summary>The quest this session tracks.</summary>
    public QuestId QuestId { get; }

    /// <summary>The character running this quest.</summary>
    public CharacterId CharacterId { get; }

    /// <summary>When this session was created.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Wall-clock deadline (UTC) by which this quest must be completed.
    /// Null means no time limit.
    /// </summary>
    public DateTime? Deadline { get; }

    /// <summary>
    /// All characters participating in this session (the primary claimant + shared party members).
    /// For non-shared quests, this contains only the primary character.
    /// </summary>
    public List<CharacterId> PartyMembers { get; } = [];

    /// <summary>Read-only view of all objective states.</summary>
    public IReadOnlyDictionary<ObjectiveId, ObjectiveState> ObjectiveStates => _objectiveStates;

    /// <summary>Whether all groups in this session are completed.</summary>
    public bool IsFullyCompleted => _groupCompletionStatus.Values.All(v => v);

    /// <summary>Whether any objective has failed (and the quest should be considered failed).</summary>
    public bool HasFailure => _objectiveStates.Values.Any(s => s.IsFailed);

    /// <summary>The stage currently being tracked. Updated when the session auto-advances.</summary>
    public int CurrentStageId => _currentStageId;

    public QuestSession(
        QuestId questId,
        CharacterId characterId,
        List<QuestObjectiveGroup> groups,
        IObjectiveEvaluatorRegistry evaluatorRegistry,
        DateTime createdAt,
        DateTime? deadline = null,
        List<CharacterId>? partyMembers = null,
        StageContext? stageContext = null)
    {
        QuestId = questId;
        CharacterId = characterId;
        _groups = groups;
        _evaluatorRegistry = evaluatorRegistry;
        CreatedAt = createdAt;
        Deadline = deadline;
        PartyMembers = partyMembers ?? [characterId];

        _allStages = stageContext?.AllStages;
        _currentStageId = stageContext?.CurrentStageId ?? 0;

        // Ensure the primary character is always in the party
        if (!PartyMembers.Contains(characterId))
            PartyMembers.Insert(0, characterId);

        InitializeObjectiveStates();
    }

    /// <summary>
    /// Processes a signal through all active objectives and returns the resulting domain events.
    /// </summary>
    public IReadOnlyList<CodexDomainEvent> ProcessSignal(QuestSignal signal)
    {
        List<CodexDomainEvent> events = [];
        DateTime now = DateTime.UtcNow;

        // Check deadline before processing any objectives
        if (Deadline.HasValue && now >= Deadline.Value)
        {
            events.Add(new QuestExpiredEvent(
                CharacterId, now, QuestId,
                Enums.ExpiryBehavior.Fail)); // Default; actual behavior resolved by the application layer
            return events;
        }

        for (int groupIndex = 0; groupIndex < _groups.Count; groupIndex++)
        {
            if (_groupCompletionStatus.GetValueOrDefault(groupIndex))
                continue; // Group already completed

            QuestObjectiveGroup group = _groups[groupIndex];

            foreach (ObjectiveDefinition definition in group.Objectives)
            {
                ObjectiveState state = _objectiveStates[definition.ObjectiveId];

                if (!state.IsActive || state.IsTerminal)
                    continue;

                IObjectiveEvaluator? evaluator = _evaluatorRegistry.GetEvaluator(definition.TypeTag);
                if (evaluator == null)
                    continue;

                int oldCount = state.CurrentCount;
                EvaluationResult result = evaluator.Evaluate(signal, definition, state);

                if (!result.StateChanged)
                    continue;

                if (result.IsFailed)
                {
                    state.IsFailed = true;
                    state.IsActive = false;
                    events.Add(new ObjectiveFailedEvent(
                        CharacterId, now, QuestId, definition.ObjectiveId,
                        result.Message ?? "Objective failed"));
                }
                else if (result.IsCompleted)
                {
                    state.IsCompleted = true;
                    state.IsActive = false;
                    events.Add(new ObjectiveCompletedEvent(
                        CharacterId, now, QuestId, definition.ObjectiveId));

                    // Check if this completion satisfies the group
                    IReadOnlyList<CodexDomainEvent> groupEvents = CheckGroupCompletion(groupIndex, group, now);
                    events.AddRange(groupEvents);
                }
                else
                {
                    // Progress event
                    events.Add(new ObjectiveProgressedEvent(
                        CharacterId, now, QuestId, definition.ObjectiveId,
                        oldCount, state.CurrentCount, definition.RequiredCount));
                }
            }

            // In Sequence mode, activate the next objective if the current one just completed
            if (group.CompletionMode == CompletionMode.Sequence)
            {
                ActivateNextInSequence(group);
            }
        }

        return events;
    }

    /// <summary>
    /// Gets the state for a specific objective.
    /// </summary>
    public ObjectiveState? GetObjectiveState(ObjectiveId objectiveId)
        => _objectiveStates.GetValueOrDefault(objectiveId);

    /// <summary>
    /// Gets whether a specific group is completed.
    /// </summary>
    public bool IsGroupCompleted(int groupIndex)
        => _groupCompletionStatus.GetValueOrDefault(groupIndex);

    private void InitializeObjectiveStates()
    {
        for (int groupIndex = 0; groupIndex < _groups.Count; groupIndex++)
        {
            QuestObjectiveGroup group = _groups[groupIndex];
            _groupCompletionStatus[groupIndex] = false;

            for (int i = 0; i < group.Objectives.Count; i++)
            {
                ObjectiveDefinition definition = group.Objectives[i];
                ObjectiveState state = new()
                {
                    ObjectiveId = definition.ObjectiveId,
                    // In Sequence mode, only the first objective starts active
                    IsActive = group.CompletionMode != CompletionMode.Sequence || i == 0
                };

                _objectiveStates[definition.ObjectiveId] = state;
                _objectiveDefinitions[definition.ObjectiveId] = definition;

                // Let the evaluator set up any custom initial state
                IObjectiveEvaluator? evaluator = _evaluatorRegistry.GetEvaluator(definition.TypeTag);
                evaluator?.Initialize(definition, state);
            }
        }
    }

    private IReadOnlyList<CodexDomainEvent> CheckGroupCompletion(
        int groupIndex, QuestObjectiveGroup group, DateTime now)
    {
        List<CodexDomainEvent> events = [];
        bool isComplete = group.CompletionMode switch
        {
            CompletionMode.All => group.Objectives.All(o =>
                _objectiveStates[o.ObjectiveId].IsCompleted),

            CompletionMode.Any => group.Objectives.Any(o =>
                _objectiveStates[o.ObjectiveId].IsCompleted),

            CompletionMode.Sequence => group.Objectives.All(o =>
                _objectiveStates[o.ObjectiveId].IsCompleted),

            _ => false
        };

        if (!isComplete) return events;

        _groupCompletionStatus[groupIndex] = true;
        events.Add(new QuestObjectiveGroupCompletedEvent(
            CharacterId, now, QuestId, groupIndex, group.DisplayName, group.CompletionStageId));

        // ── Auto-stage-advancement ──────────────────────────────────
        // Priority: Group.CompletionStageId > (all groups done) Stage.NextStageId > next numeric stage
        if (_allStages is null) return events;

        if (group.CompletionStageId.HasValue)
        {
            // Group-level branch — advance immediately when this group completes
            events.AddRange(AdvanceToStageInternal(group.CompletionStageId.Value, now));
        }
        else if (IsFullyCompleted)
        {
            // All groups done — resolve target via stage-level or numeric fallback
            int? target = ResolveNextStageId();
            if (target.HasValue)
            {
                events.AddRange(AdvanceToStageInternal(target.Value, now));
            }
        }

        return events;
    }

    /// <summary>
    /// Resolves the next stage ID using the priority chain:
    /// <c>CurrentStage.NextStageId > next numeric stage in the quest</c>.
    /// Returns null if no further stage exists (quest is at its last stage).
    /// </summary>
    private int? ResolveNextStageId()
    {
        if (_allStages is null) return null;

        QuestStage? currentStage = _allStages.FirstOrDefault(s => s.StageId == _currentStageId);

        // Explicit override on the current stage
        if (currentStage?.NextStageId is { } explicitNext) return explicitNext;

        // Fallback: next numeric stage after current
        return _allStages
            .Where(s => s.StageId > _currentStageId)
            .OrderBy(s => s.StageId)
            .FirstOrDefault()?.StageId;
    }

    /// <summary>
    /// Advances the session to a new stage: emits a <see cref="QuestStageAdvancedEvent"/>,
    /// then reinitializes objective tracking for the new stage's groups.
    /// </summary>
    private IReadOnlyList<CodexDomainEvent> AdvanceToStageInternal(int targetStageId, DateTime now)
    {
        List<CodexDomainEvent> events = [];
        int fromStage = _currentStageId;
        _currentStageId = targetStageId;

        events.Add(new QuestStageAdvancedEvent(
            CharacterId, now, QuestId, fromStage, targetStageId));

        // Grant the completed stage's rewards (if any)
        QuestStage? completedStage = _allStages?.FirstOrDefault(s => s.StageId == fromStage);
        if (completedStage is { Rewards.IsEmpty: false })
        {
            events.Add(new StageRewardsGrantedEvent(
                CharacterId, now, QuestId, fromStage, completedStage.Rewards));
        }

        // Load the new stage's objective groups and reinitialize tracking
        QuestStage? newStage = _allStages?.FirstOrDefault(s => s.StageId == targetStageId);
        if (newStage is not null)
        {
            _groups = newStage.ObjectiveGroups;
            _objectiveStates.Clear();
            _objectiveDefinitions.Clear();
            _groupCompletionStatus.Clear();
            InitializeObjectiveStates();
        }

        return events;
    }

    private void ActivateNextInSequence(QuestObjectiveGroup group)
    {
        for (int i = 0; i < group.Objectives.Count; i++)
        {
            ObjectiveState state = _objectiveStates[group.Objectives[i].ObjectiveId];

            if (state.IsCompleted)
                continue;

            if (!state.IsActive && !state.IsTerminal)
            {
                state.IsActive = true;
                break;
            }

            break; // Current objective is still active or failed
        }
    }
}
