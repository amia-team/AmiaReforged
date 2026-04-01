using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// In-memory registry for objective evaluators.
/// Used in tests and as the runtime implementation.
/// </summary>
[ServiceBinding(typeof(IObjectiveEvaluatorRegistry))]
public sealed class ObjectiveEvaluatorRegistry : IObjectiveEvaluatorRegistry
{
    private readonly Dictionary<string, IObjectiveEvaluator> _evaluators;

    public ObjectiveEvaluatorRegistry(IEnumerable<IObjectiveEvaluator> evaluators)
    {
        _evaluators = new Dictionary<string, IObjectiveEvaluator>(StringComparer.OrdinalIgnoreCase);

        foreach (IObjectiveEvaluator evaluator in evaluators)
        {
            _evaluators.TryAdd(evaluator.TypeTag, evaluator);
        }
    }

    public IObjectiveEvaluator? GetEvaluator(string typeTag)
        => _evaluators.GetValueOrDefault(typeTag);

    public IReadOnlyCollection<IObjectiveEvaluator> GetAll()
        => _evaluators.Values;
}
