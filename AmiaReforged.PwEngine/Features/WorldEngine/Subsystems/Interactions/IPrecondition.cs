using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// A composable check that must pass before an interaction can begin.
/// Implementations are stateless and reusable across interaction types.
/// </summary>
public interface IPrecondition
{
    /// <summary>Short identifier for logging / diagnostics (e.g., <c>"HasTool"</c>, <c>"HasKnowledge"</c>).</summary>
    string Type { get; }

    /// <summary>
    /// Evaluates whether <paramref name="character"/> satisfies this condition
    /// for the interaction described by <paramref name="context"/>.
    /// </summary>
    PreconditionResult Check(ICharacter character, InteractionContext context);
}
