using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;

/// <summary>
/// Domain event representing gold transferred between two personas.
/// Published after the transfer transaction is successfully recorded.
/// </summary>
public sealed record GoldTransferredEvent(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    TransactionId TransactionId,
    string? Memo,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

