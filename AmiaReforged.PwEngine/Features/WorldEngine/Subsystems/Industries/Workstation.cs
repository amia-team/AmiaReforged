using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Represents a crafting workstation type — a physical station in the game world
/// where characters perform crafting. Workstations are global (not industry-scoped)
/// and can be shared across multiple industries.
/// </summary>
public class Workstation
{
    /// <summary>
    /// Unique identifier for this workstation type.
    /// </summary>
    public required WorkstationTag Tag { get; init; }

    /// <summary>
    /// Display name (e.g., "Forge", "Alchemy Table", "Enchanting Altar").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of this workstation type.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// NWN placeable blueprint ResRef for in-world placement.
    /// Used to identify which physical placeables act as this workstation type.
    /// </summary>
    public string? PlaceableResRef { get; init; }

    /// <summary>
    /// Optional override for the spawned placeable's appearance.
    /// Row index into <c>NwGameTables.PlaceableTable</c>.
    /// When null, the blueprint's default appearance is used.
    /// </summary>
    public int? AppearanceId { get; init; }

    /// <summary>
    /// Industries that can use this workstation (informational, not enforcement).
    /// Multiple industries can share the same workstation.
    /// </summary>
    public List<IndustryTag> SupportedIndustries { get; init; } = [];
}
