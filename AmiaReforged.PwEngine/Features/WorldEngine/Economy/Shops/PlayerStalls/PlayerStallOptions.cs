using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Data required to assign ownership to a player stall.
/// </summary>
/// <param name="OwnerPersonaId">Serialized character persona identifier for the new owner.</param>
/// <param name="OwnerPlayerPersonaId">Serialized player persona identifier for the owning CD key.</param>
/// <param name="OwnerDisplayName">Display name to show on the stall.</param>
/// <param name="CoinHouseAccountId">Optional account used for settlements.</param>
/// <param name="HoldEarningsInStall">Whether the stall should retain proceeds until withdrawal.</param>
/// <param name="LeaseStartUtc">Lease start timestamp in UTC.</param>
/// <param name="NextRentDueUtc">Next rent due timestamp in UTC.</param>
public sealed record PlayerStallClaimOptions(
    string OwnerPersonaId,
    string OwnerPlayerPersonaId,
    string OwnerDisplayName,
    Guid? CoinHouseAccountId,
    bool HoldEarningsInStall,
    DateTime LeaseStartUtc,
    DateTime NextRentDueUtc);

/// <summary>
/// Descriptor used to construct a stall product listing.
/// </summary>
public sealed record PlayerStallProductDescriptor(
    long StallId,
    string ResRef,
    string Name,
    string? Description,
    int Price,
    int Quantity,
    int? BaseItemType,
    byte[] ItemData,
    string? ConsignorPersonaId,
    string? ConsignorDisplayName,
    string? Notes,
    int SortOrder,
    bool IsActive,
    DateTime ListedUtc,
    DateTime UpdatedUtc,
    string? OriginalName = null);
