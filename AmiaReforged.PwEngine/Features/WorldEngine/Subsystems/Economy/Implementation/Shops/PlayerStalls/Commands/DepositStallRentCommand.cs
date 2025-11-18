using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Command to record a deposit to a stall's escrow balance for rent payment.
/// Adds to the escrow balance and records a ledger entry.
/// </summary>
public sealed record DepositStallRentCommand : ICommand
{
    public required long StallId { get; init; }
    public required int DepositAmount { get; init; }
    public required string DepositorPersonaId { get; init; }
    public required string DepositorDisplayName { get; init; }
    public required DateTime DepositTimestamp { get; init; }

    /// <summary>
    /// Creates a validated DepositStallRentCommand.
    /// </summary>
    /// <param name="stallId">The ID of the stall</param>
    /// <param name="depositAmount">The amount to deposit (must be positive)</param>
    /// <param name="depositorPersonaId">The persona ID of the depositor</param>
    /// <param name="depositorDisplayName">The display name of the depositor</param>
    /// <param name="timestamp">The timestamp of the deposit</param>
    /// <returns>A validated command</returns>
    public static DepositStallRentCommand Create(
        long stallId,
        int depositAmount,
        string depositorPersonaId,
        string depositorDisplayName,
        DateTime timestamp)
    {
        if (stallId <= 0)
            throw new ArgumentException("Stall ID must be positive", nameof(stallId));

        if (depositAmount <= 0)
            throw new ArgumentException("Deposit amount must be positive", nameof(depositAmount));

        if (string.IsNullOrWhiteSpace(depositorPersonaId))
            throw new ArgumentException("Depositor persona ID is required", nameof(depositorPersonaId));

        if (string.IsNullOrWhiteSpace(depositorDisplayName))
            throw new ArgumentException("Depositor display name is required", nameof(depositorDisplayName));

        return new DepositStallRentCommand
        {
            StallId = stallId,
            DepositAmount = depositAmount,
            DepositorPersonaId = depositorPersonaId,
            DepositorDisplayName = depositorDisplayName,
            DepositTimestamp = timestamp
        };
    }
}

