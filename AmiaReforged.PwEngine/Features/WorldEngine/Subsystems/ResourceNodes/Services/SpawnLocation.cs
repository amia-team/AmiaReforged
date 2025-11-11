using System.Numerics;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;

/// <summary>
/// Value object representing a potential spawn location for a resource node.
/// </summary>
public class SpawnLocation
{
    public Vector3 Position { get; init; }
    public float Rotation { get; init; }
    public string NodeTag { get; init; } = string.Empty;
    public string? TriggerSource { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

