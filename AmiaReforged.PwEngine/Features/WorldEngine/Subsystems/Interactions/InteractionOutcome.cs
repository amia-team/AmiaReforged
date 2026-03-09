namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Final result of a completed interaction, carrying domain-specific payload
/// in the <see cref="Data"/> dictionary.
/// </summary>
public sealed record InteractionOutcome(
    bool Success,
    string? Message = null,
    Dictionary<string, object>? Data = null)
{
    /// <summary>Creates a successful outcome with optional payload.</summary>
    public static InteractionOutcome Succeeded(string? message = null, Dictionary<string, object>? data = null)
        => new(true, message, data);

    /// <summary>Creates a failed outcome with a reason.</summary>
    public static InteractionOutcome Failed(string message)
        => new(false, message);
}
