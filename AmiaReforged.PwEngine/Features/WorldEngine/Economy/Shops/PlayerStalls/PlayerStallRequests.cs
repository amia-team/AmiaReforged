using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Request to claim a player stall.
/// </summary>
public sealed record ClaimPlayerStallRequest(
    long StallId,
    PersonaId OwnerPersona,
    string OwnerDisplayName,
    Guid? CoinHouseAccountId,
    bool HoldEarningsInStall,
    DateTime LeaseStartUtc,
    DateTime NextRentDueUtc);

/// <summary>
/// Request to release a player stall.
/// </summary>
public sealed record ReleasePlayerStallRequest(
    long StallId,
    PersonaId Requestor,
    bool Force,
    DateTime? ReleasedUtc = null);

/// <summary>
/// Request to list a product on a player stall.
/// </summary>
public sealed record ListStallProductRequest(
    long StallId,
    string ResRef,
    string Name,
    string? Description,
    int Price,
    int Quantity,
    int? BaseItemType,
    byte[] ItemData,
    PersonaId? ConsignorPersona,
    string? ConsignorDisplayName,
    string? Notes,
    int SortOrder,
    bool IsActive,
    DateTime ListedUtc,
    DateTime UpdatedUtc);

/// <summary>
/// Service boundary for player stall operations.
/// </summary>
public interface IPlayerStallService
{
    Task<PlayerStallServiceResult> ClaimAsync(ClaimPlayerStallRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> ReleaseAsync(ReleasePlayerStallRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> ListProductAsync(ListStallProductRequest request, CancellationToken cancellationToken = default);
}
