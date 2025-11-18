using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Command to record a successful stall rent payment.
/// Deducts from escrow balance (if applicable), updates next due date, and records ledger entry.
/// </summary>
public sealed record PayStallRentCommand : ICommand
{
    public required long StallId { get; init; }
    public required int RentAmount { get; init; }
    public required RentChargeSource Source { get; init; }
    public required DateTime PaymentTimestamp { get; init; }

    /// <summary>
    /// Creates a validated PayStallRentCommand.
    /// </summary>
    /// <param name="stallId">The ID of the stall</param>
    /// <param name="rentAmount">The amount of rent paid</param>
    /// <param name="source">The source of the rent payment (coinhouse, escrow, or none)</param>
    /// <param name="timestamp">The timestamp of the payment</param>
    /// <returns>A validated command</returns>
    public static PayStallRentCommand Create(
        long stallId,
        int rentAmount,
        RentChargeSource source,
        DateTime timestamp)
    {
        if (stallId <= 0)
            throw new ArgumentException("Stall ID must be positive", nameof(stallId));

        if (rentAmount < 0)
            throw new ArgumentException("Rent amount cannot be negative", nameof(rentAmount));

        return new PayStallRentCommand
        {
            StallId = stallId,
            RentAmount = rentAmount,
            Source = source,
            PaymentTimestamp = timestamp
        };
    }
}

/// <summary>
/// The source from which rent was charged.
/// </summary>
public enum RentChargeSource
{
    /// <summary>
    /// No rent was charged (free stall or zero rent).
    /// </summary>
    None = 0,

    /// <summary>
    /// Rent was deducted from the stall's escrow balance.
    /// </summary>
    StallEscrow = 1,

    /// <summary>
    /// Rent was withdrawn from the owner's coinhouse account.
    /// </summary>
    CoinhouseAccount = 2
}

