namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Configuration for an AI archetype.
/// Can be loaded from JSON or code for designer-friendly editing.
/// </summary>
public class AiArchetypeConfig
{
    /// <summary>
    /// Unique identifier for this archetype.
    /// </summary>
    public required string ArchetypeId { get; init; }

    /// <summary>
    /// Display name for debugging/logging.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Archetype scale value (1-10).
    /// 1-3 = Melee, 4-6 = Hybrid, 7-10 = Caster
    /// </summary>
    public int ArchetypeValue { get; init; }

    /// <summary>
    /// Priority modifiers for different behavior types.
    /// </summary>
    public Dictionary<string, int> BehaviorPriorities { get; init; } = new();

    /// <summary>
    /// Custom properties for archetype-specific configuration.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

