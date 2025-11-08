using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Queries;

/// <summary>
/// Query to get all stored items for a character at a specific bank.
/// </summary>
public record GetStoredItemsQuery(
    CoinhouseTag CoinhouseTag,
    Guid CharacterId) : IQuery<List<StoredItemDto>>;

/// <summary>
/// DTO for stored item information.
/// </summary>
public record StoredItemDto(
    long ItemId,
    string Name,
    string Description,
    byte[] ItemData);
