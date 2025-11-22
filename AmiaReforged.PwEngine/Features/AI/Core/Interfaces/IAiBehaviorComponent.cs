using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Base interface for AI behavior components.
/// Components are composable units that implement specific AI actions.
/// Based on the behavior pattern from ds_ai_include.nss PerformAction() (lines 550-640).
/// </summary>
public interface IAiBehaviorComponent
{
    /// <summary>
    /// Unique identifier for this component (e.g., "melee_attack", "spell_heal").
    /// </summary>
    string ComponentId { get; }

    /// <summary>
    /// Execution priority (higher = executes first).
    /// Typical ranges: 100 = healing, 90 = buffs, 80 = attacks, 70 = movement.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if this behavior can execute in the current context.
    /// Returns false if preconditions are not met (no target, on cooldown, etc.).
    /// </summary>
    bool CanExecute(BehaviorContext context);

    /// <summary>
    /// Executes the behavior and returns the result.
    /// Should only be called after CanExecute returns true.
    /// </summary>
    BehaviorResult Execute(BehaviorContext context);
}

