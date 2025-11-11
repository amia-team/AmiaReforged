using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Events;

/// <summary>
/// Domain event representing gold deposited into a coinhouse.
/// Published after the deposit transaction is successfully recorded.
/// </summary>
public sealed record GoldDepositedEvent(
    PersonaId Depositor,
    CoinhouseTag Coinhouse,
    GoldAmount Amount,
    TransactionId TransactionId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

