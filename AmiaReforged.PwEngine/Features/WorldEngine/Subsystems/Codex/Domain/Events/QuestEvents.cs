using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;

public sealed record QuestDiscoveredEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    string QuestName,
    string Description
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record QuestStartedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    string QuestName,
    string Description
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record QuestCompletedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record QuestFailedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    string Reason
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record QuestAbandonedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record QuestStageAdvancedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    int FromStage,
    int ToStage
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Emitted when a stage with non-empty <see cref="RewardMix"/> is completed
/// (i.e., the quest has advanced past it). The handler is responsible for
/// translating the reward mix into actual character rewards (XP, gold, etc.).
/// </summary>
public sealed record StageRewardsGrantedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    int CompletedStageId,
    RewardMix Rewards
) : CodexDomainEvent(CharacterId, OccurredAt);
