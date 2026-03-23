namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// Registry for resolving <see cref="IObjectiveEvaluator"/> implementations by their type tag.
/// At runtime populated via Anvil DI; in tests populated manually.
/// </summary>
public interface IObjectiveEvaluatorRegistry
{
    /// <summary>
    /// Returns the evaluator for the given type tag, or null if not registered.
    /// </summary>
    IObjectiveEvaluator? GetEvaluator(string typeTag);

    /// <summary>
    /// Returns all registered evaluators.
    /// </summary>
    IReadOnlyCollection<IObjectiveEvaluator> GetAll();
}
