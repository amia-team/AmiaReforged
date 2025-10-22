using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.Codex.Domain.Events;

/// <summary>
/// Base record for all Codex domain events.
/// </summary>
public abstract record CodexDomainEvent(
    CharacterId CharacterId,
    DateTime OccurredAt
);
