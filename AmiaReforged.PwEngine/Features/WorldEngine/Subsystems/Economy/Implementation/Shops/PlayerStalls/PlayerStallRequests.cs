using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Request to claim a player stall.
/// </summary>
public sealed record ClaimPlayerStallRequest(
    long StallId,
    string AreaResRef,
    string PlaceableTag,
    PersonaId OwnerPersona,
    PersonaId OwnerPlayerPersona,
    string OwnerDisplayName,
    Guid? CoinHouseAccountId,
    bool HoldEarningsInStall,
    DateTime LeaseStartUtc,
    DateTime NextRentDueUtc,
    IReadOnlyCollection<PlayerStallCoOwnerRequest>? CoOwners = null);

/// <summary>
/// Request to release a player stall.
/// </summary>
public sealed record ReleasePlayerStallRequest(
    long StallId,
    PersonaId Requestor,
    bool Force,
    DateTime? ReleasedUtc = null,
    string? AreaResRef = null,
    string? PlaceableTag = null);

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
    DateTime UpdatedUtc,
    string? OriginalName = null);

/// <summary>
/// Request to update the configured price for a stall product.
/// </summary>
public sealed record UpdateStallProductPriceRequest(
    long StallId,
    long ProductId,
    PersonaId Requestor,
    int NewPrice);

/// <summary>
/// Request to update how rent is funded for a player stall.
/// </summary>
public sealed record UpdateStallRentSettingsRequest(
    long StallId,
    PersonaId Requestor,
    Guid? CoinHouseAccountId,
    bool HoldEarningsInStall);

/// <summary>
/// Request to withdraw available earnings from a player stall escrow account.
/// </summary>
public sealed record WithdrawStallEarningsRequest(
    long StallId,
    PersonaId Requestor,
    int? RequestedAmount);

/// <summary>
/// Service boundary for player stall operations.
/// </summary>
public interface IPlayerStallService
{
    Task<PlayerStallServiceResult> ClaimAsync(ClaimPlayerStallRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> ReleaseAsync(ReleasePlayerStallRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> ListProductAsync(ListStallProductRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> UpdateProductPriceAsync(UpdateStallProductPriceRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> UpdateRentSettingsAsync(UpdateStallRentSettingsRequest request, CancellationToken cancellationToken = default);

    Task<PlayerStallServiceResult> WithdrawEarningsAsync(WithdrawStallEarningsRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request payload describing a co-owner to be granted stall permissions.
/// </summary>
/// <param name="Persona">Persona identifier for the co-owner.</param>
/// <param name="DisplayName">Display name to expose in UI.</param>
/// <param name="CanManageInventory">Whether the co-owner may add/remove stock.</param>
/// <param name="CanConfigureSettings">Whether the co-owner may change stall configuration.</param>
/// <param name="CanCollectEarnings">Whether the co-owner may withdraw stall escrow.</param>
public sealed record PlayerStallCoOwnerRequest(
    PersonaId Persona,
    string DisplayName,
    bool CanManageInventory,
    bool CanConfigureSettings,
    bool CanCollectEarnings);
