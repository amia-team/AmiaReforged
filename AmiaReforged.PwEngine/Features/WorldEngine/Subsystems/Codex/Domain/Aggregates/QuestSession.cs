using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;

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
    private readonly List<QuestObjectiveGroup> _groups;
    private readonly Dictionary<int, bool> _groupCompletionStatus = new();

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

    public QuestSession(
        QuestId questId,
        CharacterId characterId,
        List<QuestObjectiveGroup> groups,
        IObjectiveEvaluatorRegistry evaluatorRegistry,
        DateTime createdAt,
        DateTime? deadline = null,
        List<CharacterId>? partyMembers = null)
    {
        QuestId = questId;
        CharacterId = characterId;
        _groups = groups;
        _evaluatorRegistry = evaluatorRegistry;
        CreatedAt = createdAt;
        Deadline = deadline;
        PartyMembers = partyMembers ?? [characterId];

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

        if (isComplete)
        {
            _groupCompletionStatus[groupIndex] = true;
            events.Add(new QuestObjectiveGroupCompletedEvent(
                CharacterId, now, QuestId, groupIndex, group.DisplayName));
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
