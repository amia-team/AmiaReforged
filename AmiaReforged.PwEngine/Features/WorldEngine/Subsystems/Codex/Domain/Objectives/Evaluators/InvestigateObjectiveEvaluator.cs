using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Models;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates investigation objectives using either a <see cref="ClueGraph"/> (evidence-based)
/// or a <see cref="StateMachineDefinition"/> (branching narrative), selected via configuration.
/// <para>
/// Clue Graph mode: player discovers clues via signals → prerequisite sets unlock deductions →
/// reaching the conclusion deduction completes the objective.
/// </para>
/// <para>
/// State Machine mode: signals trigger transitions between narrative states →
/// reaching a terminal success/failure state completes/fails the objective.
/// </para>
/// </summary>
public sealed class InvestigateObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "investigate";

    /// <summary>Config key: "clue_graph" or "state_machine".</summary>
    public const string ModeKey = "mode";

    /// <summary>Config key for the ClueGraph model (when mode is "clue_graph").</summary>
    public const string ClueGraphKey = "clue_graph";

    /// <summary>Config key for the StateMachineDefinition (when mode is "state_machine").</summary>
    public const string StateMachineKey = "state_machine";

    // Custom state keys
    private const string DiscoveredCluesKey = "discovered_clues";
    private const string UnlockedDeductionsKey = "unlocked_deductions";
    private const string CurrentSmStateKey = "current_sm_state";

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        string mode = definition.GetConfig<string>(ModeKey) ?? "clue_graph";

        if (mode == "clue_graph")
        {
            state.SetCustom(DiscoveredCluesKey, new HashSet<string>());
            state.SetCustom(UnlockedDeductionsKey, new HashSet<string>());
        }
        else if (mode == "state_machine")
        {
            StateMachineDefinition? sm = definition.GetConfig<StateMachineDefinition>(StateMachineKey);
            if (sm != null)
            {
                state.SetCustom(CurrentSmStateKey, sm.InitialStateId);
            }
        }
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        string mode = definition.GetConfig<string>(ModeKey) ?? "clue_graph";

        return mode switch
        {
            "clue_graph" => EvaluateClueGraph(signal, definition, state),
            "state_machine" => EvaluateStateMachine(signal, definition, state),
            _ => EvaluationResult.NoOp()
        };
    }

    private static EvaluationResult EvaluateClueGraph(
        QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        ClueGraph? graph = definition.GetConfig<ClueGraph>(ClueGraphKey);
        if (graph == null)
            return EvaluationResult.NoOp();

        // Only respond to clue_found signals
        if (signal.SignalType != SignalType.ClueFound)
            return EvaluationResult.NoOp();

        HashSet<string> discovered = state.GetCustom<HashSet<string>>(DiscoveredCluesKey) ?? [];
        HashSet<string> unlocked = state.GetCustom<HashSet<string>>(UnlockedDeductionsKey) ?? [];

        // Find the clue matching this signal's target tag
        Clue? clue = graph.Clues.FirstOrDefault(
            c => string.Equals(c.TriggerTag, signal.TargetTag, StringComparison.OrdinalIgnoreCase));

        if (clue == null || discovered.Contains(clue.ClueId))
            return EvaluationResult.NoOp(); // Unknown clue or already discovered

        // Discover the clue
        discovered.Add(clue.ClueId);
        state.SetCustom(DiscoveredCluesKey, discovered);

        // Check if any deductions are now unlocked
        bool anyNewDeductions = false;
        bool conclusionReached = false;

        // Iterate until no more deductions can be unlocked (cascade)
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (Deduction deduction in graph.Deductions)
            {
                if (unlocked.Contains(deduction.DeductionId))
                    continue;

                if (deduction.RequiredClueIds.All(id => discovered.Contains(id)))
                {
                    unlocked.Add(deduction.DeductionId);
                    anyNewDeductions = true;
                    changed = true;

                    // Unlock any clues this deduction provides
                    foreach (string unlockedClueId in deduction.UnlocksClueIds)
                    {
                        if (discovered.Add(unlockedClueId))
                        {
                            // New clue discovered via deduction — may cascade further
                        }
                    }

                    if (deduction.DeductionId == graph.ConclusionDeductionId)
                    {
                        conclusionReached = true;
                    }
                }
            }
        }

        state.SetCustom(UnlockedDeductionsKey, unlocked);
        state.SetCustom(DiscoveredCluesKey, discovered);

        if (conclusionReached)
        {
            state.IsCompleted = true;
            state.IsActive = false;
            return EvaluationResult.Completed("Investigation complete — conclusion reached");
        }

        string message = anyNewDeductions
            ? $"New deduction unlocked! ({discovered.Count} clues, {unlocked.Count} deductions)"
            : $"Clue discovered: {clue.Name} ({discovered.Count} clues total)";

        return EvaluationResult.Progressed(message);
    }

    private static EvaluationResult EvaluateStateMachine(
        QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        StateMachineDefinition? sm = definition.GetConfig<StateMachineDefinition>(StateMachineKey);
        if (sm == null)
            return EvaluationResult.NoOp();

        string currentStateId = state.GetCustom<string>(CurrentSmStateKey) ?? sm.InitialStateId;

        IReadOnlyList<NarrativeTransition> transitions =
            sm.GetTransitions(currentStateId, signal.SignalType, signal.TargetTag);

        if (transitions.Count == 0)
            return EvaluationResult.NoOp();

        // Take the first matching transition
        NarrativeTransition transition = transitions[0];
        state.SetCustom(CurrentSmStateKey, transition.ToStateId);

        NarrativeState? targetState = sm.States.FirstOrDefault(s => s.StateId == transition.ToStateId);

        if (targetState?.IsTerminalSuccess == true)
        {
            state.IsCompleted = true;
            state.IsActive = false;
            return EvaluationResult.Completed(
                transition.TransitionText ?? $"Reached conclusion: {targetState.Description}");
        }

        if (targetState?.IsTerminalFailure == true)
        {
            state.IsFailed = true;
            state.IsActive = false;
            return EvaluationResult.Failed(
                transition.TransitionText ?? $"Investigation failed: {targetState.Description}");
        }

        return EvaluationResult.Progressed(
            transition.TransitionText ?? $"Moved to: {targetState?.Description ?? transition.ToStateId}");
    }
}
