namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Models;

/// <summary>
/// A state in a branching narrative state machine.
/// Players transition between states via signals (dialog choices, clue discovery, etc.).
/// </summary>
public sealed class NarrativeState
{
    /// <summary>Unique identifier for this state.</summary>
    public required string StateId { get; init; }

    /// <summary>Player-visible description of the current narrative position.</summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether reaching this state completes the objective successfully.
    /// </summary>
    public bool IsTerminalSuccess { get; init; }

    /// <summary>
    /// Whether reaching this state fails the objective.
    /// </summary>
    public bool IsTerminalFailure { get; init; }

    /// <summary>Whether this is any kind of terminal state.</summary>
    public bool IsTerminal => IsTerminalSuccess || IsTerminalFailure;
}

/// <summary>
/// A transition between narrative states, triggered by a matching signal.
/// </summary>
public sealed class NarrativeTransition
{
    /// <summary>State this transition originates from.</summary>
    public required string FromStateId { get; init; }

    /// <summary>State this transition leads to.</summary>
    public required string ToStateId { get; init; }

    /// <summary>
    /// The signal type that triggers this transition (e.g., "dialog_choice", "clue_found").
    /// </summary>
    public required string SignalType { get; init; }

    /// <summary>
    /// The target tag the signal must match for this transition to fire.
    /// </summary>
    public required string TargetTag { get; init; }

    /// <summary>Optional player-visible text describing what happened during the transition.</summary>
    public string? TransitionText { get; init; }
}

/// <summary>
/// A finite state machine for branching narrative objectives.
/// Players move between states via signals; reaching a terminal state completes or fails the objective.
/// </summary>
public sealed class StateMachineDefinition
{
    /// <summary>All states in this state machine.</summary>
    public required List<NarrativeState> States { get; init; }

    /// <summary>All transitions between states.</summary>
    public required List<NarrativeTransition> Transitions { get; init; }

    /// <summary>The state ID the player starts in.</summary>
    public required string InitialStateId { get; init; }

    /// <summary>
    /// Validates that the state machine is structurally sound:
    /// - All state IDs referenced in transitions exist
    /// - The initial state exists
    /// - At least one terminal state exists
    /// - No duplicate state IDs
    /// </summary>
    public ValidationResult Validate()
    {
        HashSet<string> stateIds = new(States.Select(s => s.StateId));
        List<string> errors = [];

        if (States.Count != stateIds.Count)
            errors.Add("Duplicate state IDs found");

        if (!stateIds.Contains(InitialStateId))
            errors.Add($"Initial state '{InitialStateId}' not found in states");

        if (!States.Any(s => s.IsTerminal))
            errors.Add("No terminal states defined");

        foreach (NarrativeTransition transition in Transitions)
        {
            if (!stateIds.Contains(transition.FromStateId))
                errors.Add($"Transition references unknown source state '{transition.FromStateId}'");

            if (!stateIds.Contains(transition.ToStateId))
                errors.Add($"Transition references unknown target state '{transition.ToStateId}'");
        }

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }

    /// <summary>
    /// Finds all transitions from the given state matching the provided signal.
    /// </summary>
    public IReadOnlyList<NarrativeTransition> GetTransitions(string fromStateId, string signalType, string targetTag)
    {
        return Transitions
            .Where(t => t.FromStateId == fromStateId
                        && t.SignalType == signalType
                        && string.Equals(t.TargetTag, targetTag, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
