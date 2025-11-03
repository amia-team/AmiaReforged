using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;

/// <summary>
/// Command used to assign ownership of a player stall to a persona.
/// </summary>
public sealed record ClaimPlayerStallCommand : ICommand
{
    public required long StallId { get; init; }
    public required PersonaId OwnerPersona { get; init; }
    public required string OwnerDisplayName { get; init; }
    public Guid? CoinHouseAccountId { get; init; }
    public bool HoldEarningsInStall { get; init; }
    public DateTime LeaseStartUtc { get; init; }
    public DateTime NextRentDueUtc { get; init; }

    /// <summary>
    /// Factory method to create a validated command instance.
    /// </summary>
    public static ClaimPlayerStallCommand Create(
        long stallId,
        PersonaId ownerPersona,
        string ownerDisplayName,
        Guid? coinHouseAccountId = null,
        bool holdEarningsInStall = false,
        TimeSpan? rentInterval = null,
        DateTime? leaseStartUtc = null)
    {
        if (stallId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stallId), "Stall id must be a positive value.");
        }

        if (string.IsNullOrWhiteSpace(ownerDisplayName))
        {
            throw new ArgumentException("Owner display name is required.", nameof(ownerDisplayName));
        }

        TimeSpan interval = rentInterval ?? TimeSpan.FromDays(1);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(rentInterval), "Rent interval must be greater than zero.");
        }

        DateTime leaseStart = leaseStartUtc?.ToUniversalTime() ?? DateTime.UtcNow;

        return new ClaimPlayerStallCommand
        {
            StallId = stallId,
            OwnerPersona = ownerPersona,
            OwnerDisplayName = ownerDisplayName.Trim(),
            CoinHouseAccountId = coinHouseAccountId,
            HoldEarningsInStall = holdEarningsInStall,
            LeaseStartUtc = leaseStart,
            NextRentDueUtc = leaseStart + interval
        };
    }
}
