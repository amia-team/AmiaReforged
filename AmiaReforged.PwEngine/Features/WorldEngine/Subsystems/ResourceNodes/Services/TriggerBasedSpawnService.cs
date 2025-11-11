using System.Numerics;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;

/// <summary>
/// Service for generating spawn locations within trigger-defined resource zones.
/// Ensures fair distribution of node types and proper spacing.
/// </summary>
[ServiceBinding(typeof(TriggerBasedSpawnService))]
public class TriggerBasedSpawnService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Random _random = new();

    /// <summary>
    /// Find all resource zone triggers in an area.
    /// </summary>
    public List<NwTrigger> GetResourceTriggers(NwArea area)
    {
        return area.Objects.OfType<NwTrigger>()
            .Where(t => t.Tag == WorldConstants.ResourceNodeZoneTag)
            .ToList();
    }

    /// <summary>
    /// Generate spawn locations within a trigger, ensuring:
    /// - At least 1 of each node type (if space allows)
    /// - Fair distribution across remaining slots
    /// - Minimum spacing between nodes
    /// - Random rotations
    /// </summary>
    public List<SpawnLocation> GenerateSpawnLocations(
        NwTrigger trigger,
        List<string> nodeTags,
        int maxNodes = WorldConstants.DefaultMaxNodesPerTrigger)
    {
        if (!nodeTags.Any())
        {
            Log.Warn($"Trigger {trigger.Tag} has no node tags defined");
            return new List<SpawnLocation>();
        }

        List<SpawnLocation> locations = new List<SpawnLocation>();
        List<Vector3> occupiedPositions = new List<Vector3>();

        // Distribute node types fairly
        List<string> distributedTags = DistributeNodeTypes(nodeTags, maxNodes);

        Log.Info($"Generating {distributedTags.Count} spawn locations in trigger {trigger.Tag} " +
                 $"for tags: {string.Join(", ", nodeTags)}");

        foreach (string nodeTag in distributedTags)
        {
            SpawnLocation? location = GenerateValidSpawnLocation(
                trigger,
                nodeTag,
                occupiedPositions
            );

            if (location != null)
            {
                locations.Add(location);
                occupiedPositions.Add(location.Position);
            }
            else
            {
                Log.Warn($"Failed to generate valid spawn location for {nodeTag} in trigger {trigger.Tag}");
            }
        }

        Log.Info($"Successfully generated {locations.Count}/{distributedTags.Count} spawn locations");
        return locations;
    }

    /// <summary>
    /// Distribute node types fairly across available slots.
    /// Guarantees at least 1 of each type, then distributes remainder.
    /// </summary>
    public List<string> DistributeNodeTypes(List<string> nodeTags, int totalSlots)
    {
        List<string> distributed = new List<string>();

        // Step 1: Guarantee at least 1 of each type
        foreach (string tag in nodeTags)
        {
            distributed.Add(tag);
        }

        // Step 2: Distribute remaining slots
        int remainingSlots = totalSlots - nodeTags.Count;

        if (remainingSlots > 0)
        {
            // Distribute remaining slots evenly (or randomly)
            for (int i = 0; i < remainingSlots; i++)
            {
                string randomTag = nodeTags[_random.Next(nodeTags.Count)];
                distributed.Add(randomTag);
            }
        }
        else if (remainingSlots < 0)
        {
            // More types than slots - just take first N types
            distributed = nodeTags.Take(totalSlots).ToList();
            Log.Warn($"More node types ({nodeTags.Count}) than available slots ({totalSlots}). " +
                     $"Some types will not spawn.");
        }

        // Shuffle for randomness
        return distributed.OrderBy(_ => _random.Next()).ToList();
    }

    /// <summary>
    /// Generate a valid spawn location within the trigger, respecting spacing constraints.
    /// </summary>
    private SpawnLocation? GenerateValidSpawnLocation(
        NwTrigger trigger,
        string nodeTag,
        List<Vector3> occupiedPositions)
    {
        const int maxAttempts = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 position = GetRandomPointInTrigger(trigger);
            float rotation = (float)(_random.NextDouble() * 360);

            if (IsValidSpawnPosition(trigger.Area, position, occupiedPositions))
            {
                return new SpawnLocation
                {
                    Position = position,
                    Rotation = rotation,
                    NodeTag = nodeTag,
                    TriggerSource = trigger.Tag,
                    Metadata = new Dictionary<string, object>
                    {
                        ["attempt"] = attempt + 1,
                        ["triggerUUID"] = trigger.UUID.ToString()
                    }
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Generate a random point within the trigger's area.
    /// Note: NWN triggers don't expose polygon vertices through Anvil API easily,
    /// so we use the trigger's position with a reasonable radius for distribution.
    /// </summary>
    public Vector3 GetRandomPointInTrigger(NwTrigger trigger)
    {
        // Use trigger center with random offset
        // This creates a circular distribution around the trigger
        // In practice, level designers should size triggers appropriately
        Vector3 center = trigger.Position;
        float radius = 10f; // Default radius in meters

        // Try to get a better radius estimate from trigger bounds if available
        // For now, use a fixed radius that works well for most trigger sizes

        float angle = (float)(_random.NextDouble() * Math.PI * 2);
        float distance = (float)(_random.NextDouble() * radius);

        float x = center.X + (float)(Math.Cos(angle) * distance);
        float y = center.Y + (float)(Math.Sin(angle) * distance);
        float z = center.Z;

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Generate random point in triangle using barycentric coordinates.
    /// Currently unused but kept for future enhancement if we can access trigger geometry.
    /// </summary>
    private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = (float)Math.Sqrt(_random.NextDouble());
        float r2 = (float)_random.NextDouble();

        float alpha = 1 - r1;
        float beta = r1 * (1 - r2);
        float gamma = r1 * r2;

        return new Vector3(
            alpha * a.X + beta * b.X + gamma * c.X,
            alpha * a.Y + beta * b.Y + gamma * c.Y,
            a.Z // Use first vertex Z (NWN tile height)
        );
    }

    /// <summary>
    /// Check if a position is valid for spawning:
    /// - Walkable terrain
    /// - Minimum distance from other nodes
    /// </summary>
    public bool IsValidSpawnPosition(
        NwArea? area,
        Vector3 position,
        List<Vector3> existingPositions,
        float minDistance = WorldConstants.MinNodeSpacing)
    {
        if (area == null)
            return false;

        // Check walkability
        Location testLocation = Location.Create(area, position, 0f);
        if (!testLocation.IsWalkable)
        {
            return false;
        }

        // Check spacing from existing nodes
        foreach (Vector3 existingPos in existingPositions)
        {
            float distance = Vector3.Distance(position, existingPos);
            if (distance < minDistance)
            {
                return false;
            }
        }

        return true;
    }
}

