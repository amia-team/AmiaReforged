using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.Codex.Domain.Events;

public sealed record TraitAcquiredEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    TraitTag TraitTag,
    string AcquisitionMethod
) : CodexDomainEvent(CharacterId, OccurredAt);

