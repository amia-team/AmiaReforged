using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.Codex.Domain.Events;

public sealed record ReputationChangedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    FactionId FactionId,
    int Delta,
    string Reason
) : CodexDomainEvent(CharacterId, OccurredAt);
