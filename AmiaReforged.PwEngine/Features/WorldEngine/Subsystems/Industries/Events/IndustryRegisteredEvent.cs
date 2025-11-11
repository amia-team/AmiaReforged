using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;

/// <summary>
/// Domain event representing an industry definition registered in the system.
/// Published after an industry is successfully loaded from configuration.
/// </summary>
public sealed record IndustryRegisteredEvent(
    IndustryTag IndustryTag,
    string Name,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

