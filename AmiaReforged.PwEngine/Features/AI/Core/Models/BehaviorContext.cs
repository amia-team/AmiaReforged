using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Execution context passed to AI behavior components.
/// Contains all information needed for decision making.
/// </summary>
public class BehaviorContext
{
    /// <summary>
    /// The creature executing this behavior.
    /// </summary>
    public required NwCreature Creature { get; init; }

    /// <summary>
    /// The current target (may be null if no valid target).
    /// Uses NwGameObject to support creatures, placeables, etc.
    /// </summary>
    public NwGameObject? Target { get; init; }

    /// <summary>
    /// Cached spell list for this creature.
    /// </summary>
    public CreatureSpellCache? SpellCache { get; init; }

    /// <summary>
    /// Timestamp of this behavior execution.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

