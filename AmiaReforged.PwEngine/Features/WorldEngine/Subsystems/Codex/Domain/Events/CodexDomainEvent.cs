using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;

/// <summary>
/// Base record for all Codex domain events.
/// Implements <see cref="IDomainEvent"/> so command handlers can publish Codex
/// events on the shared event bus (F-6 CQRS audit).
/// </summary>
public abstract record CodexDomainEvent(
    CharacterId CharacterId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
