using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities.PlayerHousing;

/// <summary>
/// Represents a saved PLC layout configuration for a property.
/// Allows players to save and restore their house furnishing arrangements.
/// </summary>
public class PlcLayoutConfiguration
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// The property this layout belongs to.
    /// </summary>
    public required Guid PropertyId { get; set; }

    /// <summary>
    /// The character who created/owns this layout.
    /// </summary>
    public required Guid CharacterId { get; set; }

    /// <summary>
    /// User-defined name for this layout (e.g., "Winter Decor", "Party Setup").
    /// </summary>
    [MaxLength(64)]
    public required string Name { get; set; }

    /// <summary>
    /// When the layout was first created.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the layout was last updated.
    /// </summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The individual items in this layout.
    /// </summary>
    public List<PlcLayoutItem> Items { get; set; } = new();
}
