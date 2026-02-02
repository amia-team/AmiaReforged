using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.PlayerHousing;

/// <summary>
/// Represents a single PLC item within a saved layout configuration.
/// Stores the matching criteria and position data needed to recreate the placement.
/// </summary>
public class PlcLayoutItem
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// The parent layout configuration this item belongs to.
    /// </summary>
    public long LayoutConfigurationId { get; set; }

    [ForeignKey(nameof(LayoutConfigurationId))]
    public PlcLayoutConfiguration? LayoutConfiguration { get; set; }

    /// <summary>
    /// The resref of the placeable (used for matching inventory items).
    /// Matches the "plc_resref" local variable on furniture items.
    /// </summary>
    [MaxLength(32)]
    public required string PlcResRef { get; set; }

    /// <summary>
    /// The name of the placeable (used for matching inventory items).
    /// Matches the "plc_name" or item name on furniture items.
    /// </summary>
    [MaxLength(128)]
    public required string PlcName { get; set; }

    /// <summary>
    /// X coordinate in the area.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate in the area.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Z coordinate in the area.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Facing direction in degrees.
    /// </summary>
    public float Orientation { get; set; }

    /// <summary>
    /// Visual scale of the placeable.
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// Appearance row index from the placeable table.
    /// </summary>
    public int Appearance { get; set; }

    /// <summary>
    /// Health override for the placeable (-1 means use default).
    /// </summary>
    public int HealthOverride { get; set; } = -1;

    /// <summary>
    /// Whether the placeable is plot-flagged (indestructible).
    /// </summary>
    public bool IsPlot { get; set; }

    /// <summary>
    /// Whether the placeable is static (non-interactive).
    /// </summary>
    public bool IsStatic { get; set; }
}
