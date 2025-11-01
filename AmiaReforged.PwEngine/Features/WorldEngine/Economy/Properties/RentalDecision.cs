namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Result of evaluating a rental request.
/// </summary>
public sealed record RentalDecision(bool Success, RentalDecisionReason Reason, string? Message)
{
    public static RentalDecision Allowed() => new(true, RentalDecisionReason.None, null);

    public static RentalDecision Denied(RentalDecisionReason reason, string? message) =>
        new(false, reason, message);
}
