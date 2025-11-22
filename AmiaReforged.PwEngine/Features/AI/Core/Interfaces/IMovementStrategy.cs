using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Strategy interface for movement and positioning behaviors.
/// Implements movement logic for different combat styles (aggressive, defensive, tactical).
/// </summary>
public interface IMovementStrategy
{
    /// <summary>
    /// Unique identifier for this movement strategy.
    /// </summary>
    string StrategyId { get; }

    /// <summary>
    /// Determines the movement action for the creature.
    /// </summary>
    /// <param name="creature">The AI creature.</param>
    /// <param name="target">The current target (may be null).</param>
    /// <returns>True if movement action was queued.</returns>
    bool Move(NwCreature creature, NwGameObject? target);

    /// <summary>
    /// Calculates the optimal position for this movement strategy.
    /// </summary>
    /// <param name="creature">The AI creature.</param>
    /// <param name="target">The target to position relative to.</param>
    /// <returns>The desired position, or null if no movement needed.</returns>
    Location? GetOptimalPosition(NwCreature creature, NwGameObject target);
}

