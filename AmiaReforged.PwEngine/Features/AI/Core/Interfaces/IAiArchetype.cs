using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Defines an AI archetype that determines creature behavior patterns.
/// Archetypes compose behavior components into cohesive AI strategies.
/// </summary>
public interface IAiArchetype
{
    /// <summary>
    /// Unique identifier for this archetype (e.g., "melee", "caster", "hybrid").
    /// </summary>
    string ArchetypeId { get; }

    /// <summary>
    /// Human-readable display name for logging/debugging.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Returns the ordered list of behavior components for this archetype.
    /// Components are executed in priority order until one succeeds.
    /// </summary>
    IEnumerable<IAiBehaviorComponent> GetBehaviors();

    /// <summary>
    /// Calculates the priority for executing behaviors in the given context.
    /// Higher priority archetypes can override default behaviors.
    /// </summary>
    int GetPriority(BehaviorContext context);
}

