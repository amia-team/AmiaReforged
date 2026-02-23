using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to region-related operations including area management and regional effects.
/// </summary>
public interface IRegionSubsystem
{
    // === Region Management ===

    /// <summary>
    /// Gets region information by tag.
    /// </summary>
    Task<RegionInfo?> GetRegionAsync(string regionTag, CancellationToken ct = default);

    /// <summary>
    /// Gets all regions in the world.
    /// </summary>
    Task<List<RegionInfo>> GetAllRegionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates region properties.
    /// </summary>
    Task<CommandResult> UpdateRegionAsync(UpdateRegionCommand command, CancellationToken ct = default);

    // === Regional Effects ===

    /// <summary>
    /// Applies a regional effect to an area.
    /// </summary>
    Task<CommandResult> ApplyRegionalEffectAsync(
        string regionTag,
        string effectId,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a regional effect from an area.
    /// </summary>
    Task<CommandResult> RemoveRegionalEffectAsync(
        string regionTag,
        string effectId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active regional effects for a region.
    /// </summary>
    Task<List<RegionalEffect>> GetRegionalEffectsAsync(
        string regionTag,
        CancellationToken ct = default);

    // === Chaos System ===

    /// <summary>
    /// Gets the chaos state for a specific area. Returns area-level override if present,
    /// region default if available, or <see cref="ChaosState.Default"/> as final fallback.
    /// </summary>
    Task<ChaosState> GetChaosForAreaAsync(string areaResRef, CancellationToken ct = default);

    // === Area Registration ===

    /// <summary>
    /// Returns true if the area (by resref) is defined in any region.
    /// Unregistered areas should not receive chaos state — only mutations (profile bonuses).
    /// </summary>
    bool IsAreaInRegion(string areaResRef);

    /// <summary>
    /// Returns the region tag for the area, or null if the area is not defined in any region.
    /// </summary>
    string? GetRegionTagForArea(string areaResRef);
}

/// <summary>
/// Represents information about a region.
/// </summary>
public record RegionInfo(
    string Tag,
    string Name,
    string Description,
    RegionType Type);

/// <summary>
/// Represents a regional effect.
/// </summary>
public record RegionalEffect(
    string EffectId,
    string Name,
    string Description,
    DateTime AppliedAt,
    DateTime? ExpiresAt);

/// <summary>
/// Types of regions in the world.
/// </summary>
public enum RegionType
{
    Wilderness,
    Settlement,
    Dungeon,
    City,
    Special
}

/// <summary>
/// Command to update region properties.
/// </summary>
public record UpdateRegionCommand(
    string RegionTag,
    string? Name = null,
    string? Description = null,
    RegionType? Type = null);

