using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Meta-evaluator that wraps a set of child objectives with AND/OR/SEQUENCE semantics.
/// Allows designers to compose complex goals from simpler objective types.
/// <para>
/// Child objectives are stored in Config as a list of <see cref="ObjectiveDefinition"/>
/// and evaluated using the evaluator registry. The composite delegates signal processing
/// to each child, then checks the group's completion mode.
/// </para>
/// </summary>
[ServiceBinding(typeof(IObjectiveEvaluator))]
public sealed class CompositeObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "composite";

    /// <summary>Config key for the list of child ObjectiveDefinitions.</summary>
    public const string ChildrenKey = "children";

    /// <summary>Config key for the CompletionMode of the children.</summary>
    public const string ModeKey = "completion_mode";

    // Custom state keys
    private const string ChildStatesKey = "child_states";
    private const string InitializedKey = "composite_initialized";

    private readonly IObjectiveEvaluatorRegistry _registry;

    public CompositeObjectiveEvaluator(IObjectiveEvaluatorRegistry registry)
    {
        _registry = registry;
    }

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        List<ObjectiveDefinition>? children = definition.GetConfig<List<ObjectiveDefinition>>(ChildrenKey);
        if (children == null || children.Count == 0)
            return;

        CompletionMode mode = GetCompletionMode(definition);
        Dictionary<string, ObjectiveState> childStates = new();

        for (int i = 0; i < children.Count; i++)
        {
            ObjectiveDefinition child = children[i];
            ObjectiveState childState = new()
            {
                ObjectiveId = child.ObjectiveId,
                IsActive = mode != CompletionMode.Sequence || i == 0
            };

            IObjectiveEvaluator? evaluator = _registry.GetEvaluator(child.TypeTag);
            evaluator?.Initialize(child, childState);

            childStates[child.ObjectiveId.Value] = childState;
        }

        state.SetCustom(ChildStatesKey, childStates);
        state.SetCustom(InitializedKey, true);
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        List<ObjectiveDefinition>? children = definition.GetConfig<List<ObjectiveDefinition>>(ChildrenKey);
        if (children == null || children.Count == 0)
            return EvaluationResult.NoOp();

        Dictionary<string, ObjectiveState>? childStates =
            state.GetCustom<Dictionary<string, ObjectiveState>>(ChildStatesKey);
        if (childStates == null)
            return EvaluationResult.NoOp();

        CompletionMode mode = GetCompletionMode(definition);
        bool anyProgress = false;
        bool anyFailed = false;

        foreach (ObjectiveDefinition child in children)
        {
            if (!childStates.TryGetValue(child.ObjectiveId.Value, out ObjectiveState? childState))
                continue;

            if (!childState.IsActive || childState.IsTerminal)
                continue;

            IObjectiveEvaluator? evaluator = _registry.GetEvaluator(child.TypeTag);
            if (evaluator == null)
                continue;

            EvaluationResult result = evaluator.Evaluate(signal, child, childState);

            if (result.StateChanged)
                anyProgress = true;

            if (result.IsFailed)
                anyFailed = true;
        }

        // In Sequence mode, activate next child if current completed
        if (mode == CompletionMode.Sequence)
        {
            ActivateNextInSequence(children, childStates);
        }

        if (!anyProgress)
            return EvaluationResult.NoOp();

        // Check composite completion
        bool isComplete = mode switch
        {
            CompletionMode.All => children.All(c =>
                childStates.TryGetValue(c.ObjectiveId.Value, out ObjectiveState? cs) && cs.IsCompleted),

            CompletionMode.Any => children.Any(c =>
                childStates.TryGetValue(c.ObjectiveId.Value, out ObjectiveState? cs) && cs.IsCompleted),

            CompletionMode.Sequence => children.All(c =>
                childStates.TryGetValue(c.ObjectiveId.Value, out ObjectiveState? cs) && cs.IsCompleted),

            _ => false
        };

        if (isComplete)
        {
            state.IsCompleted = true;
            state.IsActive = false;
            return EvaluationResult.Completed("All sub-objectives completed");
        }

        // Check for failure in modes where it matters
        if (anyFailed && mode == CompletionMode.All)
        {
            // In All mode, if any child fails, the composite fails
            state.IsFailed = true;
            state.IsActive = false;
            return EvaluationResult.Failed("A required sub-objective has failed");
        }

        if (anyFailed && mode == CompletionMode.Sequence)
        {
            state.IsFailed = true;
            state.IsActive = false;
            return EvaluationResult.Failed("A sub-objective in the sequence has failed");
        }

        int completed = children.Count(c =>
            childStates.TryGetValue(c.ObjectiveId.Value, out ObjectiveState? cs) && cs.IsCompleted);
        state.CurrentCount = completed;

        return EvaluationResult.Progressed($"{completed}/{children.Count} sub-objectives completed");
    }

    private static CompletionMode GetCompletionMode(ObjectiveDefinition definition)
    {
        object? modeObj = definition.Config.GetValueOrDefault(ModeKey);

        if (modeObj is CompletionMode cm)
            return cm;

        if (modeObj is string modeStr && Enum.TryParse<CompletionMode>(modeStr, true, out CompletionMode parsed))
            return parsed;

        return CompletionMode.All;
    }

    private static void ActivateNextInSequence(
        List<ObjectiveDefinition> children, Dictionary<string, ObjectiveState> childStates)
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (!childStates.TryGetValue(children[i].ObjectiveId.Value, out ObjectiveState? cs))
                continue;

            if (cs.IsCompleted)
                continue;

            if (!cs.IsActive && !cs.IsTerminal)
            {
                cs.IsActive = true;
                break;
            }

            break;
        }
    }
}
