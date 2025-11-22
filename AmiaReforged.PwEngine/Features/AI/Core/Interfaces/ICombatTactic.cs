using Anvil.API;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Interface for combat tactics (spells, attacks, abilities).
/// Implements specific combat actions from ds_ai_include.nss DoSpellCast(), DoAttack(), etc.
/// </summary>
public interface ICombatTactic
{
    /// <summary>
    /// Unique identifier for this tactic (e.g., "melee_attack", "fireball").
    /// </summary>
    string TacticId { get; }

    /// <summary>
    /// Priority of this tactic relative to others.
    /// Higher priority tactics are attempted first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if this tactic can be executed against the target.
    /// </summary>
    /// <param name="creature">The AI creature executing the tactic.</param>
    /// <param name="target">The target of the tactic.</param>
    /// <returns>True if tactic can be used.</returns>
    bool CanExecute(NwCreature creature, NwGameObject? target);

    /// <summary>
    /// Executes the tactic against the target.
    /// </summary>
    /// <param name="creature">The AI creature executing the tactic.</param>
    /// <param name="target">The target of the tactic.</param>
    /// <returns>Result of the tactic execution.</returns>
    TacticResult Execute(NwCreature creature, NwGameObject target);
}

