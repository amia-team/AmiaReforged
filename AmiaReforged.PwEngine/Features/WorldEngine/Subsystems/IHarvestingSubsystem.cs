using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to harvesting-related operations including resource nodes and gathering.
/// </summary>
public interface IHarvestingSubsystem
{
    // === Resource Node Management ===

    /// <summary>
    /// Spawns a resource node in the world.
    /// </summary>
    Task<CommandResult> SpawnResourceNodeAsync(string nodeType, string areaTag, float x, float y, float z, CancellationToken ct = default);

    /// <summary>
    /// Despawns a resource node from the world.
    /// </summary>
    Task<CommandResult> DespawnResourceNodeAsync(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Gets information about a spawned resource node.
    /// </summary>
    Task<SpawnedNode?> GetResourceNodeAsync(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active resource nodes in an area.
    /// </summary>
    Task<List<SpawnedNode>> GetAreaResourceNodesAsync(string areaTag, CancellationToken ct = default);

    // === Harvesting Operations ===

    /// <summary>
    /// Performs a harvest action on a resource node.
    /// </summary>
    Task<HarvestResult> HarvestResourceAsync(CharacterId characterId, string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a character can harvest from a specific node.
    /// </summary>
    Task<bool> CanHarvestAsync(
        CharacterId characterId,
        string nodeId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the harvest context for a character and node.
    /// </summary>
    Task<HarvestContext?> GetHarvestContextAsync(
        CharacterId characterId,
        string nodeId,
        CancellationToken ct = default);

    // === Harvest History ===

    /// <summary>
    /// Gets harvest history for a character.
    /// </summary>
    Task<List<HarvestHistoryEntry>> GetHarvestHistoryAsync(
        CharacterId characterId,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the last harvest time for a character on a specific node.
    /// </summary>
    Task<DateTime?> GetLastHarvestTimeAsync(
        CharacterId characterId,
        string nodeId,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a harvest history entry.
/// </summary>
public record HarvestHistoryEntry(
    string NodeId,
    string ResourceType,
    int QuantityHarvested,
    DateTime HarvestedAt);

