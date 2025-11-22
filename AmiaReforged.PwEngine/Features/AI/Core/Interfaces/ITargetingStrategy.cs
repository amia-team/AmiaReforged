using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Strategy interface for target selection and validation.
/// Implements target acquisition logic from GetTarget() in ds_ai_include.nss (lines 301-358).
/// </summary>
public interface ITargetingStrategy
{
    /// <summary>
    /// Selects a target for the creature, considering the current target.
    /// Returns null if no valid targets are available.
    /// </summary>
    /// <param name="creature">The AI creature selecting a target.</param>
    /// <param name="currentTarget">The currently targeted enemy (may be null).</param>
    /// <returns>A valid target or null if none available.</returns>
    NwGameObject? SelectTarget(NwCreature creature, NwGameObject? currentTarget);

    /// <summary>
    /// Determines if the creature should switch from current target to new target.
    /// Considers attention span, PC preference, and target validity.
    /// </summary>
    /// <param name="creature">The AI creature.</param>
    /// <param name="currentTarget">The current target.</param>
    /// <param name="newTarget">The potential new target.</param>
    /// <returns>True if should switch to new target.</returns>
    bool ShouldSwitchTarget(NwCreature creature, NwGameObject currentTarget, NwGameObject newTarget);
}

