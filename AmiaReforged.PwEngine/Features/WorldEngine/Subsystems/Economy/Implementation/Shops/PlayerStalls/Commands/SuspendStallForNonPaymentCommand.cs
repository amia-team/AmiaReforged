using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Command to suspend a stall when rent cannot be paid.
/// Grants a grace period before releasing ownership.
/// </summary>
public sealed record SuspendStallForNonPaymentCommand : ICommand
{
    public required long StallId { get; init; }
    public required string Reason { get; init; }
    public required DateTime SuspensionTimestamp { get; init; }
    public required TimeSpan GracePeriod { get; init; }

    /// <summary>
    /// Creates a validated SuspendStallForNonPaymentCommand.
    /// </summary>
    /// <param name="stallId">The ID of the stall</param>
    /// <param name="reason">The reason for suspension</param>
    /// <param name="timestamp">The timestamp of suspension</param>
    /// <param name="gracePeriod">The grace period before ownership is released</param>
    /// <returns>A validated command</returns>
    public static SuspendStallForNonPaymentCommand Create(
        long stallId,
        string reason,
        DateTime timestamp,
        TimeSpan gracePeriod)
    {
        if (stallId <= 0)
            throw new ArgumentException("Stall ID must be positive", nameof(stallId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        if (gracePeriod <= TimeSpan.Zero)
            throw new ArgumentException("Grace period must be positive", nameof(gracePeriod));

        return new SuspendStallForNonPaymentCommand
        {
            StallId = stallId,
            Reason = reason,
            SuspensionTimestamp = timestamp,
            GracePeriod = gracePeriod
        };
    }
}

