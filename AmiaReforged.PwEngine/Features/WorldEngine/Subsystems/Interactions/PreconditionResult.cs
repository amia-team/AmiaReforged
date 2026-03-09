namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Value object returned by <see cref="IPrecondition.Check"/> indicating whether
/// the precondition passed and, if not, why.
/// </summary>
public readonly record struct PreconditionResult(bool Passed, string? FailureMessage = null)
{
    /// <summary>Creates a passing result.</summary>
    public static PreconditionResult Success() => new(true);

    /// <summary>Creates a failing result with a player-visible reason.</summary>
    public static PreconditionResult Fail(string message) => new(false, message);
}
