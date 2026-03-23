using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// Strategy interface for evaluating a specific type of quest objective.
/// Each implementation handles one objective style (kill, collect, investigate, escort, etc.).
/// Implementations are stateless singletons — all mutable state lives in <see cref="ObjectiveState"/>.
/// </summary>
public interface IObjectiveEvaluator
{
    /// <summary>
    /// Unique tag identifying this evaluator type (e.g., "kill", "collect", "investigate").
    /// Used to match <see cref="ObjectiveDefinition.TypeTag"/> to the correct evaluator.
    /// </summary>
    string TypeTag { get; }

    /// <summary>
    /// Initializes the <see cref="ObjectiveState.CustomState"/> for a new objective instance.
    /// Called once when a quest session is created. Evaluators should set up any
    /// required state structures (e.g., clue graph, state machine initial state).
    /// </summary>
    /// <param name="definition">The objective definition with configuration.</param>
    /// <param name="state">The mutable state to initialize.</param>
    void Initialize(ObjectiveDefinition definition, ObjectiveState state);

    /// <summary>
    /// Evaluates whether the given signal advances, completes, or fails this objective.
    /// Called for each active objective when a signal is processed by the quest session.
    /// </summary>
    /// <param name="signal">The incoming game event signal.</param>
    /// <param name="definition">The objective definition with configuration.</param>
    /// <param name="state">The mutable objective state to update.</param>
    /// <returns>The result of evaluation.</returns>
    EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state);
}
