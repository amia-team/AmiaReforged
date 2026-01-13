using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Static information describing a stall for presentation.
/// </summary>
public sealed record PlayerStallSummary(
    long StallId,
    string StallName,
    string? Description,
    string? SettlementName,
    string? Notice);

/// <summary>
/// Viewer-specific context when inspecting a stall.
/// </summary>
public sealed record PlayerStallBuyerContext(
    PersonaId BuyerPersona,
    string BuyerDisplayName,
    int GoldOnHand);

/// <summary>
/// Product details as shown in the buyer interface.
/// </summary>
public sealed record PlayerStallProductView(
    long ProductId,
    string DisplayName,
    int Price,
    int QuantityAvailable,
    bool IsSoldOut,
    bool IsPurchasable,
    string? Tooltip,
    string? OriginalName = null,
    int? BaseItemType = null,
    string? ItemTypeName = null,
    byte[]? ItemData = null);

/// <summary>
/// Snapshot rendered in the buyer window.
/// </summary>
public sealed record PlayerStallBuyerSnapshot(
    PlayerStallSummary Summary,
    PlayerStallBuyerContext Buyer,
    IReadOnlyList<PlayerStallProductView> Products,
    string? FeedbackMessage = null,
    Color? FeedbackColor = null,
    bool FeedbackVisible = false);

/// <summary>
/// Window wiring details used when presenting the buyer interface.
/// </summary>
public sealed record PlayerStallBuyerWindowConfig(
    long StallId,
    PersonaId BuyerPersona,
    string Title,
    PlayerStallBuyerSnapshot InitialSnapshot,
    string CloseButtonLabel = "Leave Stall");

/// <summary>
/// Purchase request raised by the buyer presenter.
/// </summary>
public sealed record PlayerStallPurchaseRequest(
    Guid SessionId,
    long StallId,
    long ProductId,
    PersonaId BuyerPersona,
    int Quantity = 1);

/// <summary>
/// Result of attempting to purchase an item from a stall.
/// </summary>
public sealed record PlayerStallPurchaseResult(
    bool Success,
    string? Message,
    Color? MessageColor,
    PlayerStallBuyerSnapshot? UpdatedSnapshot)
{
    public static PlayerStallPurchaseResult Fail(string message, Color? color = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure result requires a message.", nameof(message));
        }

        return new PlayerStallPurchaseResult(false, message, color, null);
    }

    public static PlayerStallPurchaseResult Ok(PlayerStallBuyerSnapshot snapshot, string? message = null, Color? color = null)
    {
        return new PlayerStallPurchaseResult(true, message, color, snapshot);
    }
}

/// <summary>
/// Callback delegates registered by a buyer session.
/// </summary>
public sealed record PlayerStallBuyerEventCallbacks(
    Func<PlayerStallBuyerSnapshot, Task> OnSnapshot,
    Func<PlayerStallPurchaseResult, Task> OnPurchaseResult)
{
    public static PlayerStallBuyerEventCallbacks Empty => new(
        _ => Task.CompletedTask,
        _ => Task.CompletedTask);
}

/// <summary>
/// Seller-specific context for managing a stall.
/// </summary>
public sealed record PlayerStallSellerContext(
    PersonaId SellerPersona,
    string SellerDisplayName);

/// <summary>
/// Product details shown in the seller management interface.
/// </summary>
public sealed record PlayerStallSellerProductView(
    long ProductId,
    string DisplayName,
    int Price,
    int QuantityAvailable,
    bool IsActive,
    bool IsSoldOut,
    int SortOrder,
    string? Tooltip,
    bool CanAdjustPrice,
    int? BaseItemType = null,
    string? ItemTypeName = null,
    byte[]? ItemData = null);

/// <summary>
/// Player-held item that can be consigned to the stall.
/// </summary>
public sealed record PlayerStallSellerInventoryItemView(
    string ObjectId,
    string DisplayName,
    string ResRef,
    int Quantity,
    bool IsStackable,
    string? Description = null,
    int? BaseItemType = null);

/// <summary>
/// Snapshot rendered in the seller window.
/// </summary>
public sealed record PlayerStallSellerSnapshot(
    PlayerStallSummary Summary,
    PlayerStallSellerContext Seller,
    IReadOnlyList<PlayerStallSellerProductView> Products,
    IReadOnlyList<PlayerStallSellerInventoryItemView> Inventory,
    string? FeedbackMessage = null,
    Color? FeedbackColor = null,
    bool FeedbackVisible = false,
    long? SelectedProductId = null,
    bool RentFromCoinhouse = false,
    bool RentToggleVisible = false,
    bool RentToggleEnabled = false,
    string? RentToggleTooltip = null,
    bool HoldEarningsInStall = false,
    bool HoldEarningsToggleVisible = false,
    bool HoldEarningsToggleEnabled = false,
    string? HoldEarningsToggleTooltip = null,
    string? HoldEarningsToggleLabel = null,
    int EscrowBalance = 0,
    int CurrentPeriodGrossProfits = 0,
    bool EarningsRowVisible = false,
    bool WithdrawEnabled = false,
    bool WithdrawAllEnabled = false,
    string? EarningsTooltip = null,
    bool DepositEnabled = false,
    string? DepositTooltip = null,
    IReadOnlyList<PlayerStallLedgerEntryView>? LedgerEntries = null,
    IReadOnlyList<PlayerStallMemberView>? Members = null,
    bool IsOwner = false,
    bool CanManageMembers = false,
    string? CustomDisplayName = null,
    bool RenameEnabled = false);

/// <summary>
/// Window wiring details used when presenting the seller interface.
/// </summary>
public sealed record PlayerStallSellerWindowConfig(
    long StallId,
    PersonaId SellerPersona,
    string Title,
    PlayerStallSellerSnapshot InitialSnapshot,
    string CloseButtonLabel = "Close");

/// <summary>
/// Price update request raised by the seller presenter.
/// </summary>
public sealed record PlayerStallSellerPriceRequest(
    Guid SessionId,
    long StallId,
    long ProductId,
    PersonaId SellerPersona,
    int NewPrice);

/// <summary>
/// Result of attempting a seller-side stall operation.
/// </summary>
public sealed record PlayerStallSellerOperationResult(
    bool Success,
    string? Message,
    Color? MessageColor,
    PlayerStallSellerSnapshot? Snapshot)
{
    public static PlayerStallSellerOperationResult Fail(string message, Color? color = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure result requires a message.", nameof(message));
        }

        return new PlayerStallSellerOperationResult(false, message, color, null);
    }

    public static PlayerStallSellerOperationResult Ok(PlayerStallSellerSnapshot snapshot, string? message = null, Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new PlayerStallSellerOperationResult(true, message, color, snapshot);
    }
}

/// <summary>
/// Callback delegates registered by a seller session.
/// </summary>
public sealed record PlayerStallSellerEventCallbacks(
    Func<PlayerStallSellerSnapshot, Task> OnSnapshot,
    Func<PlayerStallSellerOperationResult, Task> OnOperationResult)
{
    public static PlayerStallSellerEventCallbacks Empty => new(
        _ => Task.CompletedTask,
        _ => Task.CompletedTask);
}

/// <summary>
/// Request raised by the seller UI to list a held item for sale.
/// </summary>
public sealed record PlayerStallSellerListItemRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona,
    string ItemObjectId,
    int Price);

/// <summary>
/// Request raised by the seller UI to reclaim an existing listing.
/// </summary>
public sealed record PlayerStallSellerRetrieveProductRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona,
    long ProductId);

/// <summary>
/// Request raised by the seller UI to deposit gold into the stall's escrow for rent payment.
/// </summary>
public sealed record PlayerStallDepositRequest(
    Guid SessionId,
    long StallId,
    PersonaId DepositorPersona,
    string DepositorDisplayName,
    int DepositAmount);

/// <summary>
/// Payment option details shown when a player claims a stall.
/// </summary>
public sealed record RentStallPaymentOptionViewModel(
    RentalPaymentMethod Method,
    string ButtonLabel,
    bool Visible,
    bool Enabled,
    string StatusText,
    string Tooltip);

/// <summary>
/// Configuration used to render the stall rent window.
/// </summary>
public sealed record RentStallWindowConfig(
    string Title,
    string StallName,
    string StallDescription,
    string RentCostText,
    TimeSpan Timeout,
    RentStallPaymentOptionViewModel? DirectPaymentOption,
    RentStallPaymentOptionViewModel? CoinhousePaymentOption,
    Func<RentalPaymentMethod, Task<RentStallSubmissionResult>> OnConfirm)
{
    public string? SettlementName { get; init; }
    public Func<Task>? OnCancel { get; init; }
    public Func<Task>? OnTimeout { get; init; }
    public Func<Task>? OnClosed { get; init; }
}

/// <summary>
/// Result communicated back to the rent window when the player chooses a payment option.
/// </summary>
public sealed record RentStallSubmissionResult(
    bool Success,
    string Message,
    RentStallFeedbackKind FeedbackKind,
    bool CloseWindow,
    RentStallPaymentOptionViewModel? DirectOptionUpdate = null,
    RentStallPaymentOptionViewModel? CoinhouseOptionUpdate = null)
{
    public static RentStallSubmissionResult SuccessResult(
        string message,
        bool closeWindow = true,
        RentStallPaymentOptionViewModel? directOptionUpdate = null,
        RentStallPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(true, message, RentStallFeedbackKind.Success, closeWindow, directOptionUpdate, coinhouseOptionUpdate);

    public static RentStallSubmissionResult Error(
        string message,
        bool closeWindow = false,
        RentStallPaymentOptionViewModel? directOptionUpdate = null,
        RentStallPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(false, message, RentStallFeedbackKind.Error, closeWindow, directOptionUpdate, coinhouseOptionUpdate);

    public static RentStallSubmissionResult Info(
        string message,
        bool closeWindow = false,
        RentStallPaymentOptionViewModel? directOptionUpdate = null,
        RentStallPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(false, message, RentStallFeedbackKind.Info, closeWindow, directOptionUpdate, coinhouseOptionUpdate);
}

/// <summary>
/// Visual feedback classification for the rent stall window.
/// </summary>
public enum RentStallFeedbackKind
{
    Info,
    Success,
    Error
}

/// <summary>
/// Request raised by the seller UI to toggle how rent payments are funded.
/// </summary>
public sealed record PlayerStallRentSourceRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona,
    bool RentFromCoinhouse);

/// <summary>
/// Request raised by the seller UI to update how stall earnings are retained.
/// </summary>
public sealed record PlayerStallHoldEarningsRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona,
    bool HoldEarningsInStall);

/// <summary>
/// Request raised by the seller UI to rename the stall.
/// </summary>
public sealed record PlayerStallRenameRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona,
    string? CustomDisplayName);

/// <summary>
/// Request raised by the seller UI to withdraw stall escrow.
/// </summary>
public sealed record PlayerStallWithdrawRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona,
    int? RequestedAmount);

/// <summary>
/// Normalized ledger entry presented in stall management interfaces.
/// </summary>
public sealed record PlayerStallLedgerEntryView(
    long EntryId,
    DateTime OccurredUtc,
    PlayerStallLedgerEntryType EntryType,
    int Amount,
    string Currency,
    string? Description,
    long? TransactionId,
    string? MetadataJson);

/// <summary>
/// Request raised by the seller UI to add a member to the stall.
/// </summary>
public sealed record PlayerStallAddMemberRequest(
    Guid SessionId,
    long StallId,
    PersonaId RequestorPersona,
    PersonaId MemberPersona,
    string MemberDisplayName);

/// <summary>
/// Request raised by the seller UI to remove a member from the stall.
/// </summary>
public sealed record PlayerStallRemoveMemberRequest(
    Guid SessionId,
    long StallId,
    PersonaId RequestorPersona,
    string MemberPersonaId);

/// <summary>
/// View model for a stall member displayed in the UI.
/// </summary>
public sealed record PlayerStallMemberView(
    string PersonaId,
    string DisplayName,
    bool CanManageInventory,
    bool CanConfigureSettings,
    bool CanCollectEarnings,
    bool IsOwner,
    bool CanRemove);

/// <summary>
/// Request raised by the seller UI to close the stall and retrieve all items.
/// Items that do not fit in the player's inventory are transferred to the Market Reeve lockup.
/// Escrow balance is withdrawn to the player's gold.
/// </summary>
public sealed record PlayerStallCloseAndRetrieveAllRequest(
    Guid SessionId,
    long StallId,
    PersonaId SellerPersona);

/// <summary>
/// Result of the close stall and retrieve all operation.
/// </summary>
public sealed record PlayerStallCloseAndRetrieveAllResult(
    bool Success,
    string? Message,
    Color? MessageColor,
    int ItemsReturned,
    int ItemsSentToReeve,
    int GoldWithdrawn)
{
    public static PlayerStallCloseAndRetrieveAllResult Fail(string message, Color? color = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure result requires a message.", nameof(message));
        }

        return new PlayerStallCloseAndRetrieveAllResult(false, message, color, 0, 0, 0);
    }

    public static PlayerStallCloseAndRetrieveAllResult Ok(
        int itemsReturned,
        int itemsSentToReeve,
        int goldWithdrawn,
        string? message = null,
        Color? color = null)
    {
        return new PlayerStallCloseAndRetrieveAllResult(true, message, color, itemsReturned, itemsSentToReeve, goldWithdrawn);
    }
}
