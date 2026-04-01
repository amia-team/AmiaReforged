using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;

/// <summary>
/// Raised when an objective's progress counter advances (but is not yet complete).
/// </summary>
public sealed record ObjectiveProgressedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    ObjectiveId ObjectiveId,
    int OldCount,
    int NewCount,
    int RequiredCount
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when an objective is fully satisfied.
/// </summary>
public sealed record ObjectiveCompletedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    ObjectiveId ObjectiveId
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when an objective fails (e.g., escort NPC died).
/// </summary>
public sealed record ObjectiveFailedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    ObjectiveId ObjectiveId,
    string Reason
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when all objectives in a group are satisfied according to their completion mode.
/// </summary>
public sealed record QuestObjectiveGroupCompletedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    int GroupIndex,
    string GroupName,
    int? CompletionStageId
) : CodexDomainEvent(CharacterId, OccurredAt);
