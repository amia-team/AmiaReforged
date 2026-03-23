using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// Defines a single objective within a quest — what must be achieved
/// and how an evaluator should interpret signals against it.
/// Immutable after creation; runtime progress is tracked in <see cref="ObjectiveState"/>.
/// </summary>
public class ObjectiveDefinition
{
    /// <summary>
    /// Unique identifier for this objective within its quest.
    /// </summary>
    public required ObjectiveId ObjectiveId { get; init; }

    /// <summary>
    /// Evaluator type tag (e.g., "kill", "collect", "investigate").
    /// Resolved via <see cref="Objectives.IObjectiveEvaluatorRegistry"/> to find the
    /// correct <see cref="Objectives.IObjectiveEvaluator"/> implementation.
    /// </summary>
    public required string TypeTag { get; init; }

    /// <summary>
    /// Player-visible description of what needs to be done (e.g., "Kill 5 goblins").
    /// </summary>
    public required string DisplayText { get; init; }

    /// <summary>
    /// The tag of the target entity this objective watches for
    /// (creature tag, item tag, area resref, NPC tag, etc.).
    /// Interpretation depends on the evaluator type.
    /// </summary>
    public string? TargetTag { get; init; }

    /// <summary>
    /// Number of times the target event must occur to satisfy this objective.
    /// For counter-based objectives (kill, collect). Ignored by evaluators
    /// that don't use simple counting (investigate, escort).
    /// </summary>
    public int RequiredCount { get; init; } = 1;

    /// <summary>
    /// Evaluator-specific configuration data.
    /// Examples:
    /// <list type="bullet">
    ///   <item>Kill: (no extra config needed beyond TargetTag + RequiredCount)</item>
    ///   <item>Investigate (ClueGraph): clue nodes, deduction edges, conclusion id</item>
    ///   <item>Investigate (StateMachine): states, transitions, terminal states</item>
    ///   <item>Escort: waypoint list, max-distance, fail-on-death flag</item>
    ///   <item>Composite: child objective definitions, completion mode</item>
    /// </list>
    /// </summary>
    public Dictionary<string, object> Config { get; init; } = new();

    /// <summary>
    /// Retrieves a typed config value, or default if not present.
    /// </summary>
    public T? GetConfig<T>(string key)
    {
        if (!Config.TryGetValue(key, out object? value))
            return default;

        if (value is T typed)
            return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
