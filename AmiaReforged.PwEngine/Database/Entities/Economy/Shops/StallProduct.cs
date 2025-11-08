using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class StallProduct
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Parent stall that owns this inventory listing.
    /// </summary>
    public long StallId { get; set; }

    [ForeignKey(nameof(StallId))]
    public PlayerStall? Stall { get; set; }

    [MaxLength(64)]
    public required string ResRef { get; set; }

    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public string? OriginalName { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// The gold cost per unit as configured by the stall owner.
    /// </summary>
    public required int Price { get; set; }

    /// <summary>
    /// Quantity currently available for purchase.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional NWN base item type (2da row) for filtering/reporting.
    /// </summary>
    public int? BaseItemType { get; set; }

    public required byte[] ItemData { get; set; }

    [MaxLength(256)]
    public string? ConsignedByPersonaId { get; set; }

    [MaxLength(255)]
    public string? ConsignedByDisplayName { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime ListedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? SoldOutUtc { get; set; }
}
