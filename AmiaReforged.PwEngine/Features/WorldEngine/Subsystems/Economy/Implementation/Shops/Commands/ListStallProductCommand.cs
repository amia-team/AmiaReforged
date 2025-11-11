using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;

/// <summary>
/// Command used to list an item for sale within a player stall.
/// </summary>
public sealed record ListStallProductCommand : ICommand
{
    public required long StallId { get; init; }
    public required string ResRef { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Price { get; init; }
    public required int Quantity { get; init; }
    public int? BaseItemType { get; init; }
    public required byte[] ItemData { get; init; }
    public PersonaId? ConsignorPersona { get; init; }
    public string? ConsignorDisplayName { get; init; }
    public string? Notes { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime ListedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }

    /// <summary>
    /// Creates a validated command for listing a stall product.
    /// </summary>
    public static ListStallProductCommand Create(
        long stallId,
        string resRef,
        string name,
        byte[] itemData,
        int price,
        int quantity,
        PersonaId? consignorPersona = null,
        string? consignorDisplayName = null,
        string? description = null,
        string? notes = null,
        int? baseItemType = null,
        int sortOrder = 0,
        bool isActive = true,
        DateTime? timestampUtc = null)
    {
        if (stallId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stallId), "Stall id must be a positive value.");
        }

        if (string.IsNullOrWhiteSpace(resRef))
        {
            throw new ArgumentException("ResRef is required.", nameof(resRef));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (itemData is null || itemData.Length == 0)
        {
            throw new ArgumentException("Item data is required.", nameof(itemData));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        DateTime timestamp = timestampUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        byte[] itemDataClone = (byte[])itemData.Clone();

        return new ListStallProductCommand
        {
            StallId = stallId,
            ResRef = resRef.Trim(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Price = price,
            Quantity = quantity,
            BaseItemType = baseItemType,
            ItemData = itemDataClone,
            ConsignorPersona = consignorPersona,
            ConsignorDisplayName = string.IsNullOrWhiteSpace(consignorDisplayName) ? null : consignorDisplayName.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            SortOrder = sortOrder,
            IsActive = isActive,
            ListedUtc = timestamp,
            UpdatedUtc = timestamp
        };
    }
}
