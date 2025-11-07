using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;

/// <summary>
/// Command to evict a tenant from a rentable property, deleting their placeables and clearing property state.
/// </summary>
public sealed record EvictPropertyCommand(RentablePropertySnapshot Property) : ICommand;
