namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Determines how a group of objectives must be satisfied.
/// </summary>
public enum CompletionMode
{
    /// <summary>
    /// All objectives in the group must be completed.
    /// </summary>
    All = 0,

    /// <summary>
    /// At least one objective in the group must be completed.
    /// </summary>
    Any = 1,

    /// <summary>
    /// Objectives must be completed in the order they are defined.
    /// Each objective only becomes active after its predecessor completes.
    /// </summary>
    Sequence = 2
}
