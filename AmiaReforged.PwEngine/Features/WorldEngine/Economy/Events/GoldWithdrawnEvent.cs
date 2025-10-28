using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;

/// <summary>
/// Domain event representing gold withdrawn from a coinhouse.
/// Published after the withdrawal transaction is successfully recorded.
/// </summary>
public sealed record GoldWithdrawnEvent(
    PersonaId Withdrawer,
    CoinhouseTag Coinhouse,
    GoldAmount Amount,
    TransactionId TransactionId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

