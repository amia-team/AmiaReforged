using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.Codex.Domain.Events;

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
