using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IInventoryPort
{
    Task<bool> HasItemsAsync(Guid actorId, IReadOnlyList<Quantity> requirements, CancellationToken ct = default);
    Task ConsumeAsync(Guid actorId, IReadOnlyList<Quantity> inputs, CancellationToken ct = default);
    Task ProduceAsync(Guid actorId, IReadOnlyList<Quantity> outputs, CancellationToken ct = default);
}