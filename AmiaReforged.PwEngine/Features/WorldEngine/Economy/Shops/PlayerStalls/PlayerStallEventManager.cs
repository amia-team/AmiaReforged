using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Coordinates real-time stall updates between presenters and backend workflows.
/// </summary>
[ServiceBinding(typeof(PlayerStallEventManager))]
[ServiceBinding(typeof(IPlayerStallEventBroadcaster))]
public sealed class PlayerStallEventManager : IPlayerStallEventBroadcaster
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly RuntimeCharacterService _characters;
    private readonly IPlayerStallService _stallService;
    private readonly ICoinhouseRepository _coinhouses;
    private readonly Dictionary<Guid, BuyerSubscription> _buyerSessions = new();
    private readonly Dictionary<long, HashSet<Guid>> _stallBuyers = new();
    private readonly Dictionary<Guid, SellerSubscription> _sellerSessions = new();
    private readonly Dictionary<long, HashSet<Guid>> _stallSellers = new();
    private readonly object _syncRoot = new();

    public PlayerStallEventManager(
        IPlayerShopRepository shops,
        RuntimeCharacterService characters,
        IPlayerStallService stallService,
        ICoinhouseRepository coinhouses)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        _stallService = stallService ?? throw new ArgumentNullException(nameof(stallService));
        _coinhouses = coinhouses ?? throw new ArgumentNullException(nameof(coinhouses));
    }

    /// <summary>
    /// Registers a buyer session for stall update notifications.
    /// </summary>
    public Guid RegisterBuyerSession(long stallId, PersonaId persona, PlayerStallBuyerEventCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(callbacks);

        Guid sessionId = Guid.NewGuid();

        lock (_syncRoot)
        {
            _buyerSessions[sessionId] = new BuyerSubscription(stallId, persona, callbacks);

            if (!_stallBuyers.TryGetValue(stallId, out HashSet<Guid>? sessions))
            {
                sessions = new HashSet<Guid>();
                _stallBuyers[stallId] = sessions;
            }

            sessions.Add(sessionId);
        }

        return sessionId;
    }

    /// <summary>
    /// Unregisters a buyer session.
    /// </summary>
    public void UnregisterBuyerSession(Guid sessionId)
    {
        lock (_syncRoot)
        {
            if (!_buyerSessions.Remove(sessionId, out BuyerSubscription? subscription) || subscription is null)
            {
                return;
            }

            if (_stallBuyers.TryGetValue(subscription.StallId, out HashSet<Guid>? sessions))
            {
                sessions.Remove(sessionId);
                if (sessions.Count == 0)
                {
                    _stallBuyers.Remove(subscription.StallId);
                }
            }
        }
    }

    /// <summary>
    /// Registers a seller session for stall update notifications.
    /// </summary>
    public Guid RegisterSellerSession(long stallId, PersonaId persona, PlayerStallSellerEventCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(callbacks);

        Guid sessionId = Guid.NewGuid();

        lock (_syncRoot)
        {
            _sellerSessions[sessionId] = new SellerSubscription(stallId, persona, callbacks);

            if (!_stallSellers.TryGetValue(stallId, out HashSet<Guid>? sessions))
            {
                sessions = new HashSet<Guid>();
                _stallSellers[stallId] = sessions;
            }

            sessions.Add(sessionId);
        }

        return sessionId;
    }

    /// <summary>
    /// Unregisters a seller session.
    /// </summary>
    public void UnregisterSellerSession(Guid sessionId)
    {
        lock (_syncRoot)
        {
            if (!_sellerSessions.Remove(sessionId, out SellerSubscription? subscription) || subscription is null)
            {
                return;
            }

            if (_stallSellers.TryGetValue(subscription.StallId, out HashSet<Guid>? sessions))
            {
                sessions.Remove(sessionId);
                if (sessions.Count == 0)
                {
                    _stallSellers.Remove(subscription.StallId);
                }
            }
        }
    }

    /// <summary>
    /// Dispatches an updated snapshot to every buyer viewing the specified stall.
    /// </summary>
    public Task PublishSnapshotAsync(long stallId, PlayerStallBuyerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<Func<PlayerStallBuyerSnapshot, Task>> callbacks = CollectSnapshotCallbacks(stallId);
        return InvokeSnapshotCallbacksAsync(callbacks, snapshot);
    }

    /// <summary>
    /// Sends a purchase result to a specific buyer session.
    /// </summary>
    public Task PublishPurchaseResultAsync(Guid sessionId, PlayerStallPurchaseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Func<PlayerStallPurchaseResult, Task>? callback = null;

        lock (_syncRoot)
        {
            if (_buyerSessions.TryGetValue(sessionId, out BuyerSubscription? subscription) && subscription is not null)
            {
                callback = subscription.Callbacks.OnPurchaseResult;
            }
        }

        return callback is null ? Task.CompletedTask : SafeInvokeAsync(callback, result);
    }

    /// <summary>
    /// Handles a purchase request raised by a buyer presenter.
    /// </summary>
    public async Task<PlayerStallPurchaseResult> RequestPurchaseAsync(PlayerStallPurchaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool foundSubscription;
        BuyerSubscription? subscription;
        lock (_syncRoot)
        {
            foundSubscription = _buyerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (!foundSubscription || subscription is null)
        {
            return PlayerStallPurchaseResult.Fail("Your stall session is no longer valid.", ColorConstants.Orange);
        }

        if (subscription.StallId != request.StallId)
        {
            return PlayerStallPurchaseResult.Fail("Your stall session is no longer valid.", ColorConstants.Orange);
        }

        if (subscription.Persona != request.BuyerPersona)
        {
            return PlayerStallPurchaseResult.Fail("We couldn't verify your stall session identity.", ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.BuyerPersona, out Guid personaGuid))
        {
            return PlayerStallPurchaseResult.Fail("We couldn't verify your persona for that purchase.", ColorConstants.Orange);
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            return PlayerStallPurchaseResult.Fail("You must be logged in as that persona to make purchases.", ColorConstants.Orange);
        }

        NwCreature? buyerCreature = await ResolveActiveCreatureAsync(player).ConfigureAwait(false);
        if (buyerCreature is null)
        {
            return PlayerStallPurchaseResult.Fail("You must be possessing your character to make purchases.", ColorConstants.Orange);
        }

        PlayerStall? stall = _shops.GetShopById(request.StallId);
        if (stall is null)
        {
            return PlayerStallPurchaseResult.Fail("This stall is no longer available.", ColorConstants.Orange);
        }

        if (!IsStallOpen(stall))
        {
            return PlayerStallPurchaseResult.Fail("This stall is currently closed for business.", ColorConstants.Orange);
        }

        StallProduct? product = _shops.GetProductById(request.StallId, request.ProductId);
        if (product is null || !product.IsActive)
        {
            return PlayerStallPurchaseResult.Fail("That item is no longer for sale.", ColorConstants.Orange);
        }

        int quantity = Math.Max(request.Quantity, 1);
        if (quantity > product.Quantity)
        {
            return PlayerStallPurchaseResult.Fail("Another buyer just claimed the last one.", ColorConstants.Orange);
        }

        if (quantity > 1)
        {
            return PlayerStallPurchaseResult.Fail("Bulk purchases are not yet supported.", ColorConstants.Orange);
        }

    int unitPrice = Math.Max(product.Price, 0);
    int totalPrice = unitPrice * quantity;
    bool productWillBeDepleted = product.Quantity - quantity <= 0;

        bool paymentCaptured = false;

        if (totalPrice > 0)
        {
            if (!await TryWithdrawGoldAsync(buyerCreature, totalPrice).ConfigureAwait(false))
            {
                return PlayerStallPurchaseResult.Fail("You cannot afford that purchase.", ColorConstants.Orange);
            }

            paymentCaptured = true;
        }

        NwItem? deliveredItem = null;

        try
        {
            deliveredItem = await StallProductRestorer.RestoreItemAsync(product, buyerCreature).ConfigureAwait(false);
            if (deliveredItem is null)
            {
                if (paymentCaptured)
                {
                    await RefundGoldAsync(buyerCreature, totalPrice).ConfigureAwait(false);
                }

                return PlayerStallPurchaseResult.Fail("The stallkeeper cannot produce that item right now.", ColorConstants.Orange);
            }

            DateTime now = DateTime.UtcNow;
            int escrowAmount = totalPrice;
            int depositAmount = 0;

            bool updated = _shops.UpdateStallAndProduct(stall.Id, product.Id, (persistedStall, persistedProduct) =>
            {
                if (!persistedProduct.IsActive || persistedProduct.Quantity < quantity)
                {
                    return false;
                }

                persistedProduct.Quantity -= quantity;
                if (persistedProduct.Quantity <= 0)
                {
                    persistedProduct.Quantity = 0;
                    persistedProduct.SoldOutUtc ??= now;
                }

                persistedStall.LifetimeGrossSales += totalPrice;
                persistedStall.LifetimeNetEarnings += totalPrice;

                if (persistedStall.HoldEarningsInStall || persistedStall.CoinHouseAccountId is null)
                {
                    persistedStall.EscrowBalance += escrowAmount;
                }
                else
                {
                    persistedStall.EscrowBalance += escrowAmount;
                }

                return true;
            });

            if (!updated)
            {
                if (paymentCaptured)
                {
                    await RefundGoldAsync(buyerCreature, totalPrice).ConfigureAwait(false);
                }

                if (deliveredItem is not null)
                {
                    await DestroyItemAsync(deliveredItem).ConfigureAwait(false);
                }

                return PlayerStallPurchaseResult.Fail("Another buyer just claimed that item.", ColorConstants.Orange);
            }

            _shops.SaveTransaction(new StallTransaction
            {
                StallId = stall.Id,
                StallProductId = product.Id,
                BuyerPersonaId = request.BuyerPersona.ToString(),
                BuyerDisplayName = await ResolveBuyerDisplayNameAsync(buyerCreature, player).ConfigureAwait(false),
                Quantity = quantity,
                GrossAmount = totalPrice,
                EscrowAmount = escrowAmount,
                DepositAmount = depositAmount,
                FeeAmount = 0,
                OccurredAtUtc = now
            });

            if (productWillBeDepleted)
            {
                _shops.RemoveProductFromShop(stall.Id, product.Id);
            }

            PlayerStall? refreshedStall = _shops.GetShopWithMembers(stall.Id) ?? stall;
            List<StallProduct> refreshedProducts = _shops.ProductsForShop(stall.Id) ?? new List<StallProduct>();

            PlayerStallBuyerSnapshot snapshot = await BuildSnapshotAsync(
                refreshedStall,
                refreshedProducts,
                request.BuyerPersona,
                player,
                buyerCreature).ConfigureAwait(false);

            await PublishSellerSnapshotsAsync(
                stall.Id,
                refreshedStall,
                refreshedProducts).ConfigureAwait(false);

            string message = BuildSuccessMessage(product, totalPrice);
            return PlayerStallPurchaseResult.Ok(snapshot, message, ColorConstants.Lime);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled error completing stall purchase for stall {StallId}, product {ProductId}.",
                request.StallId,
                request.ProductId);

            if (paymentCaptured)
            {
                await RefundGoldAsync(buyerCreature, totalPrice).ConfigureAwait(false);
            }

            if (deliveredItem is not null)
            {
                await DestroyItemAsync(deliveredItem).ConfigureAwait(false);
            }

            return PlayerStallPurchaseResult.Fail("We couldn't complete that purchase.", ColorConstants.Orange);
        }
    }

    /// <summary>
    /// Returns a fresh seller snapshot for the specified session.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestSellerSnapshotAsync(Guid sessionId)
    {
        SellerSubscription? subscription;

        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(sessionId, out subscription);
        }

        if (subscription is null)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        PlayerStall? stall = _shops.GetShopWithMembers(subscription.StallId);
        if (stall is null)
        {
            return PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);
        }

        List<StallProduct> products = _shops.ProductsForShop(subscription.StallId) ?? new List<StallProduct>();
        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            stall,
            products,
            subscription.Persona,
            null).ConfigureAwait(false);

        return PlayerStallSellerOperationResult.Ok(snapshot);
    }

    /// <summary>
    /// Handles seller price update requests raised by the presenter.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestUpdatePriceAsync(PlayerStallSellerPriceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SellerSubscription? subscription;
        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (subscription is null || subscription.StallId != request.StallId || subscription.Persona != request.SellerPersona)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.SellerPersona, out Guid personaGuid))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't verify your persona for that action.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be logged in as that persona to manage the stall.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        UpdateStallProductPriceRequest serviceRequest = new(
            request.StallId,
            request.ProductId,
            request.SellerPersona,
            request.NewPrice);

        PlayerStallServiceResult serviceResult = await _stallService
            .UpdateProductPriceAsync(serviceRequest)
            .ConfigureAwait(false);

        if (!serviceResult.Success)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                serviceResult.ErrorMessage ?? "Failed to update stall product price.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? updatedStall = _shops.GetShopWithMembers(request.StallId);
        if (updatedStall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        List<StallProduct> products = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();

        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            updatedStall,
            products,
            request.SellerPersona,
            request.ProductId).ConfigureAwait(false);
        PlayerStallSellerOperationResult result = PlayerStallSellerOperationResult.Ok(
            snapshot,
            "Price updated.",
            ColorConstants.Lime);

        await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, result).ConfigureAwait(false);

        await PublishSellerSnapshotsAsync(
            request.StallId,
            updatedStall,
            products,
            request.SessionId).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Handles seller requests to change how stall rent is funded.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestUpdateRentSourceAsync(PlayerStallRentSourceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SellerSubscription? subscription;
        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (subscription is null || subscription.StallId != request.StallId || subscription.Persona != request.SellerPersona)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.SellerPersona, out Guid personaGuid))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't verify your persona for that action.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be logged in as that persona to manage the stall.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? stall = _shops.GetShopWithMembers(request.StallId);
        if (stall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        Guid? coinhouseAccountId = null;

        if (request.RentFromCoinhouse)
        {
            if (!TryResolveCoinhouseTag(stall, out CoinhouseTag coinhouseTag))
            {
                PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                    "This stall is not linked to a coinhouse; rent can only use stall earnings.",
                    ColorConstants.Orange);

                await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
                return failure;
            }

            Guid accountId = PersonaAccountId.ForCoinhouse(request.SellerPersona, coinhouseTag);
            CoinhouseAccountDto? account = await _coinhouses
                .GetAccountForAsync(accountId, CancellationToken.None)
                .ConfigureAwait(false);

            if (account is null)
            {
                PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                    "Open or join a coinhouse account in this settlement to enable automatic rent payments.",
                    ColorConstants.Orange);

                await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
                return failure;
            }

            coinhouseAccountId = account.Id;
        }

        UpdateStallRentSettingsRequest serviceRequest = new(
            request.StallId,
            request.SellerPersona,
            coinhouseAccountId,
            !request.RentFromCoinhouse);

        PlayerStallServiceResult serviceResult = await _stallService
            .UpdateRentSettingsAsync(serviceRequest)
            .ConfigureAwait(false);

        if (!serviceResult.Success)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                serviceResult.ErrorMessage ?? "Failed to update stall rent settings.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? updatedStall = _shops.GetShopWithMembers(request.StallId);
        if (updatedStall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        List<StallProduct> products = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();

        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            updatedStall,
            products,
            request.SellerPersona,
            null).ConfigureAwait(false);

        string message = request.RentFromCoinhouse
            ? "Rent will now use your coinhouse account."
            : "Rent will now use stall earnings.";

        PlayerStallSellerOperationResult result = PlayerStallSellerOperationResult.Ok(
            snapshot,
            message,
            ColorConstants.Lime);

        await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, result).ConfigureAwait(false);

        await PublishSellerSnapshotsAsync(
            request.StallId,
            updatedStall,
            products,
            request.SessionId).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Handles seller requests to update whether stall earnings are retained in escrow.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestUpdateHoldEarningsAsync(PlayerStallHoldEarningsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SellerSubscription? subscription;
        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (subscription is null || subscription.StallId != request.StallId || subscription.Persona != request.SellerPersona)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.SellerPersona, out Guid personaGuid))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't verify your persona for that action.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be logged in as that persona to manage the stall.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? stall = _shops.GetShopWithMembers(request.StallId);
        if (stall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        string personaId = request.SellerPersona.ToString();

        bool canConfigure = string.Equals(stall.OwnerPersonaId, personaId, StringComparison.OrdinalIgnoreCase);

        if (!canConfigure && stall.Members is not null)
        {
            foreach (PlayerStallMember member in stall.Members.Where(m => m is not null))
            {
                if (member.RevokedUtc.HasValue)
                {
                    continue;
                }

                if (!string.Equals(member.PersonaId, personaId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                canConfigure = member.CanConfigureSettings;
                break;
            }
        }

        if (!canConfigure)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You do not have permission to change how this stall handles earnings.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!request.HoldEarningsInStall)
        {
            if (!TryResolveCoinhouseTag(stall, out _))
            {
                PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                    "This stall is not linked to a coinhouse; profits must remain in escrow.",
                    ColorConstants.Orange);

                await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
                return failure;
            }

            if (stall.CoinHouseAccountId is null)
            {
                PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                    "Link your coinhouse account in this settlement to automatically deposit profits.",
                    ColorConstants.Orange);

                await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
                return failure;
            }
        }

        UpdateStallRentSettingsRequest serviceRequest = new(
            request.StallId,
            request.SellerPersona,
            stall.CoinHouseAccountId,
            request.HoldEarningsInStall);

        PlayerStallServiceResult serviceResult = await _stallService
            .UpdateRentSettingsAsync(serviceRequest)
            .ConfigureAwait(false);

        if (!serviceResult.Success)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                serviceResult.ErrorMessage ?? "Failed to update stall earnings handling.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? updatedStall = _shops.GetShopWithMembers(request.StallId);
        if (updatedStall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        List<StallProduct> products = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();

        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            updatedStall,
            products,
            request.SellerPersona,
            null).ConfigureAwait(false);

        string message = request.HoldEarningsInStall
            ? "Stall profits will now remain in escrow until you withdraw them."
            : "Stall profits will now deposit to your coinhouse account as sales complete.";

        PlayerStallSellerOperationResult result = PlayerStallSellerOperationResult.Ok(
            snapshot,
            message,
            ColorConstants.Lime);

        await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, result).ConfigureAwait(false);

        await PublishSellerSnapshotsAsync(
            request.StallId,
            updatedStall,
            products,
            request.SessionId).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Handles seller requests to withdraw stall earnings.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestWithdrawEarningsAsync(PlayerStallWithdrawRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SellerSubscription? subscription;
        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (subscription is null || subscription.StallId != request.StallId || subscription.Persona != request.SellerPersona)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.SellerPersona, out Guid personaGuid))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't verify your persona for that action.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be logged in as that persona to manage the stall.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        NwCreature? sellerCreature = await ResolveActiveCreatureAsync(player).ConfigureAwait(false);
        if (sellerCreature is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be possessing your character to withdraw earnings.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? stall = _shops.GetShopWithMembers(request.StallId);
        if (stall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        string personaId = request.SellerPersona.ToString();
        bool canCollect = string.Equals(stall.OwnerPersonaId, personaId, StringComparison.OrdinalIgnoreCase);

        if (!canCollect && stall.Members is not null)
        {
            foreach (PlayerStallMember member in stall.Members.Where(m => m is not null))
            {
                if (member.RevokedUtc.HasValue)
                {
                    continue;
                }

                if (!string.Equals(member.PersonaId, personaId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                canCollect = member.CanCollectEarnings;
                break;
            }
        }

        if (!canCollect)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You do not have permission to withdraw stall earnings.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (request.RequestedAmount is int requestedAmount && requestedAmount <= 0)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "Withdrawal amount must be greater than zero.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        WithdrawStallEarningsRequest serviceRequest = new(
            request.StallId,
            request.SellerPersona,
            request.RequestedAmount);

        PlayerStallServiceResult serviceResult = await _stallService
            .WithdrawEarningsAsync(serviceRequest)
            .ConfigureAwait(false);

        if (!serviceResult.Success)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                serviceResult.ErrorMessage ?? "Failed to withdraw stall earnings.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (serviceResult.Data is not { } data ||
            !data.TryGetValue("amount", out object? amountObject) ||
            amountObject is not int amount ||
            amount <= 0)
        {
            Log.Warn("Withdrawal response for stall {StallId} did not include a valid amount.", request.StallId);

            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't determine how much gold to withdraw.",
                ColorConstants.Red);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        bool wasPartial = data.TryGetValue("partial", out object? partialObject) && partialObject is bool partial && partial;

        PlayerStall? updatedStall = _shops.GetShopWithMembers(request.StallId);
        if (updatedStall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        await RefundGoldAsync(sellerCreature, amount).ConfigureAwait(false);

        List<StallProduct> products = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();

        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            updatedStall,
            products,
            request.SellerPersona,
            null).ConfigureAwait(false);

        string message = wasPartial
            ? string.Format(CultureInfo.InvariantCulture, "Withdrew {0:n0} gp (limited to available stall earnings).", amount)
            : string.Format(CultureInfo.InvariantCulture, "Withdrew {0:n0} gp from stall earnings.", amount);

        PlayerStallSellerOperationResult result = PlayerStallSellerOperationResult.Ok(
            snapshot,
            message,
            ColorConstants.Lime);

        await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, result).ConfigureAwait(false);

        await PublishSellerSnapshotsAsync(
            request.StallId,
            updatedStall,
            products,
            request.SessionId).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Handles seller requests to list an inventory item for sale.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestListInventoryItemAsync(PlayerStallSellerListItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SellerSubscription? subscription;
        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (subscription is null || subscription.StallId != request.StallId || subscription.Persona != request.SellerPersona)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.SellerPersona, out Guid personaGuid))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't verify your persona for that action.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be logged in as that persona to manage the stall.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (request.Price <= 0)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "Listing price must be greater than zero.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? stall = _shops.GetShopWithMembers(request.StallId);
        if (stall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        NwCreature? sellerCreature = await ResolveActiveCreatureAsync(player).ConfigureAwait(false);
        if (sellerCreature is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be possessing your character to list items.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (string.IsNullOrWhiteSpace(request.ItemObjectId))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't locate that inventory item.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        NwItem? item;
        await NwTask.SwitchToMainThread();
        try
        {
            item = NWScript.StringToObject(request.ItemObjectId).ToNwObject<NwItem>();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to hydrate inventory item {ObjectId} for stall listing.", request.ItemObjectId);
            item = null;
        }

        if (item is null || !item.IsValid)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "That inventory item is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (item.Possessor != sellerCreature)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must hold the item to list it for sale.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!PlayerStallInventoryPolicy.IsItemAllowed(item))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "That item cannot be listed for sale.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        string resRef = item.ResRef?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resRef))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "That item is missing a valid resource reference.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        string displayName = string.IsNullOrWhiteSpace(item.Name) ? resRef : item.Name.Trim();
        string? description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
        int quantity = Math.Max(item.StackSize, 1);
        int? baseItemType = item.BaseItem is null ? null : (int)item.BaseItem.ItemType;

        Json serializedItem = NWScript.ObjectToJson(item);
        string payload = serializedItem.Dump();
        byte[] itemData = Encoding.UTF8.GetBytes(payload);

        List<StallProduct> existingProducts = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();
        int sortOrder = existingProducts.Count == 0 ? 0 : existingProducts.Max(p => p.SortOrder) + 1;
        string consignorDisplayName = ResolveSellerDisplayName(stall, request.SellerPersona);
        DateTime timestamp = DateTime.UtcNow;

        ListStallProductRequest serviceRequest = new(
            request.StallId,
            resRef,
            displayName,
            description,
            request.Price,
            quantity,
            baseItemType,
            itemData,
            request.SellerPersona,
            consignorDisplayName,
            null,
            sortOrder,
            true,
            timestamp,
            timestamp);

        PlayerStallServiceResult serviceResult = await _stallService
            .ListProductAsync(serviceRequest)
            .ConfigureAwait(false);

        if (!serviceResult.Success)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                serviceResult.ErrorMessage ?? "Failed to list that item.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        long? newProductId = null;
        if (serviceResult.Data is not null && serviceResult.Data.TryGetValue("productId", out object? productIdObj))
        {
            try
            {
                newProductId = Convert.ToInt64(productIdObj, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to parse new product id after listing item on stall {StallId}.", request.StallId);
            }
        }

        PlayerStall? updatedStall = _shops.GetShopWithMembers(request.StallId);
        if (updatedStall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        List<StallProduct> updatedProducts = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();

        await NwTask.SwitchToMainThread();
        if (item.IsValid)
        {
            item.Destroy();
        }

        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            updatedStall,
            updatedProducts,
            request.SellerPersona,
            newProductId).ConfigureAwait(false);

        string message = string.Format(
            CultureInfo.InvariantCulture,
            "Listed {0} for {1:n0} gp.",
            displayName,
            request.Price);

        PlayerStallSellerOperationResult result = PlayerStallSellerOperationResult.Ok(
            snapshot,
            message,
            ColorConstants.Lime);

        await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, result).ConfigureAwait(false);

        await PublishSellerSnapshotsAsync(
            request.StallId,
            updatedStall,
            updatedProducts,
            request.SessionId).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Handles seller requests to reclaim an existing listing.
    /// </summary>
    public async Task<PlayerStallSellerOperationResult> RequestRetrieveProductAsync(PlayerStallSellerRetrieveProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SellerSubscription? subscription;
        lock (_syncRoot)
        {
            _sellerSessions.TryGetValue(request.SessionId, out subscription);
        }

        if (subscription is null || subscription.StallId != request.StallId || subscription.Persona != request.SellerPersona)
        {
            return PlayerStallSellerOperationResult.Fail(
                "Your stall session is no longer valid.",
                ColorConstants.Orange);
        }

        if (!TryResolvePersonaGuid(request.SellerPersona, out Guid personaGuid))
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't verify your persona for that action.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be logged in as that persona to manage the stall.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStall? stall = _shops.GetShopWithMembers(request.StallId);
        if (stall is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "This stall is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        StallProduct? product = _shops.GetProductById(request.StallId, request.ProductId);
        if (product is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "That listing is no longer available.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        PlayerStallDomainResult<bool> domainResult = aggregate.TryReclaimProduct(request.SellerPersona.ToString(), product);
        if (!domainResult.Success)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                domainResult.ErrorMessage ?? "You cannot manage that listing.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        if (product.Quantity <= 0)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "That listing no longer has any stock to reclaim.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        NwCreature? sellerCreature = await ResolveActiveCreatureAsync(player).ConfigureAwait(false);
        if (sellerCreature is null)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "You must be possessing your character to reclaim listings.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        bool flagged = _shops.UpdateStallAndProduct(request.StallId, product.Id, (persistedStall, persistedProduct) =>
        {
            PlayerStallAggregate updateAggregate = PlayerStallAggregate.FromEntity(persistedStall);
            PlayerStallDomainResult<bool> updateResult = updateAggregate.TryReclaimProduct(request.SellerPersona.ToString(), persistedProduct);

            if (!updateResult.Success)
            {
                return false;
            }

            if (persistedProduct.Quantity <= 0)
            {
                return false;
            }

            if (!persistedProduct.IsActive)
            {
                return false;
            }

            persistedProduct.IsActive = false;
            return true;
        });

        if (!flagged)
        {
            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't update that listing just yet.",
                ColorConstants.Orange);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        product.IsActive = false;

        NwItem? restoredItem = await StallProductRestorer.RestoreItemAsync(product, sellerCreature).ConfigureAwait(false);
        if (restoredItem is null)
        {
            bool reverted = _shops.UpdateStallAndProduct(request.StallId, product.Id, (persistedStall, persistedProduct) =>
            {
                if (persistedProduct.Id != product.Id || persistedProduct.StallId != request.StallId)
                {
                    return false;
                }

                persistedProduct.IsActive = true;
                return true;
            });

            if (reverted)
            {
                product.IsActive = true;
            }

            PlayerStallSellerOperationResult failure = PlayerStallSellerOperationResult.Fail(
                "We couldn't return that item right now.",
                ColorConstants.Red);

            await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, failure).ConfigureAwait(false);
            return failure;
        }

        ApplyReclaimedItemMetadata(restoredItem, stall, product);

        _shops.RemoveProductFromShop(request.StallId, product.Id);

        PlayerStall? updatedStall = _shops.GetShopWithMembers(request.StallId) ?? stall;
        List<StallProduct> updatedProducts = _shops.ProductsForShop(request.StallId) ?? new List<StallProduct>();

        PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
            updatedStall,
            updatedProducts,
            request.SellerPersona,
            null).ConfigureAwait(false);

        string productName = string.IsNullOrWhiteSpace(product.Name) ? product.ResRef : product.Name;
        string message = string.Format(CultureInfo.InvariantCulture, "Returned {0} to your inventory.", productName);

        PlayerStallSellerOperationResult result = PlayerStallSellerOperationResult.Ok(
            snapshot,
            message,
            ColorConstants.Lime);

        await PublishSellerOperationAsync(subscription.Callbacks.OnOperationResult, result).ConfigureAwait(false);

        await PublishSellerSnapshotsAsync(
            request.StallId,
            updatedStall,
            updatedProducts,
            request.SessionId).ConfigureAwait(false);

        return result;
    }

    private List<Func<PlayerStallBuyerSnapshot, Task>> CollectSnapshotCallbacks(long stallId)
    {
        List<Func<PlayerStallBuyerSnapshot, Task>> callbacks = new();

        lock (_syncRoot)
        {
            if (!_stallBuyers.TryGetValue(stallId, out HashSet<Guid>? sessions))
            {
                return callbacks;
            }

            foreach (Guid sessionId in sessions)
            {
                if (_buyerSessions.TryGetValue(sessionId, out BuyerSubscription? subscription) && subscription is not null)
                {
                    callbacks.Add(subscription.Callbacks.OnSnapshot);
                }
            }
        }

        return callbacks;
    }

    private static async Task InvokeSnapshotCallbacksAsync(
        IEnumerable<Func<PlayerStallBuyerSnapshot, Task>> callbacks,
        PlayerStallBuyerSnapshot snapshot)
    {
        foreach (Func<PlayerStallBuyerSnapshot, Task> callback in callbacks)
        {
            try
            {
                await callback(snapshot).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Buyer snapshot callback threw an exception.");
            }
        }
    }

    private static async Task SafeInvokeAsync(
        Func<PlayerStallPurchaseResult, Task> callback,
        PlayerStallPurchaseResult result)
    {
        try
        {
            await callback(result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Buyer purchase-result callback threw an exception.");
        }
    }

    private static async Task PublishSellerOperationAsync(
        Func<PlayerStallSellerOperationResult, Task> callback,
        PlayerStallSellerOperationResult result)
    {
        try
        {
            await callback(result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Seller operation callback threw an exception.");
        }
    }

    private static bool TryResolvePersonaGuid(PersonaId persona, out Guid guid)
    {
        try
        {
            guid = PersonaId.ToGuid(persona);
            return true;
        }
        catch (Exception)
        {
            guid = Guid.Empty;
            return false;
        }
    }

    private static bool IsStallOpen(PlayerStall stall)
    {
        return stall.IsActive && stall.SuspendedUtc is null;
    }

    private static async Task<NwCreature?> ResolveActiveCreatureAsync(NwPlayer player)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = player.ControlledCreature ?? player.LoginCreature;
        if (creature is null || !creature.IsValid)
        {
            return null;
        }

        return creature;
    }

    private static async Task<bool> TryWithdrawGoldAsync(NwCreature buyer, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        await NwTask.SwitchToMainThread();

        if (buyer is not { IsValid: true })
        {
            return false;
        }

        uint required = (uint)amount;
        if (buyer.Gold < required)
        {
            return false;
        }

        buyer.Gold -= required;
        return true;
    }

    private static async Task RefundGoldAsync(NwCreature buyer, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        await NwTask.SwitchToMainThread();

        if (buyer is not { IsValid: true })
        {
            return;
        }

        buyer.Gold += (uint)amount;
    }

    private static async Task<NwItem?> TryCreateItemAsync(StallProduct product, NwCreature owner)
    {
        await NwTask.SwitchToMainThread();

        if (owner is not { IsValid: true })
        {
            return null;
        }

        Location? location = owner.Location;
        if (location is null)
        {
            return null;
        }

        try
        {
            string jsonText = Encoding.UTF8.GetString(product.ItemData);
            Json json = Json.Parse(jsonText);
            NwItem? restored = json.ToNwObject<NwItem>(location, owner);
            return restored;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to restore player stall item for product {ProductId}.", product.Id);
            return null;
        }
    }

    private static async Task DestroyItemAsync(NwItem item)
    {
        await NwTask.SwitchToMainThread();

        if (item.IsValid)
        {
            item.Destroy();
        }
    }

    private static async Task<string> ResolveBuyerDisplayNameAsync(NwCreature creature, NwPlayer player)
    {
        await NwTask.SwitchToMainThread();

        if (creature is { IsValid: true } && !string.IsNullOrWhiteSpace(creature.Name))
        {
            return creature.Name;
        }

        if (player.IsValid && !string.IsNullOrWhiteSpace(player.PlayerName))
        {
            return player.PlayerName;
        }

        return "Unknown buyer";
    }

    private static async Task<int> GetGoldOnHandAsync(NwCreature creature)
    {
        await NwTask.SwitchToMainThread();

        if (creature is not { IsValid: true })
        {
            return 0;
        }

        uint gold = creature.Gold;
        return gold >= int.MaxValue ? int.MaxValue : (int)gold;
    }

    private async Task<PlayerStallBuyerSnapshot> BuildSnapshotAsync(
        PlayerStall stall,
        IReadOnlyList<StallProduct> products,
        PersonaId persona,
        NwPlayer player,
        NwCreature creature)
    {
        string stallName = BuildStallName(stall);
        string? notice = BuildStallNotice(stall);

        string buyerName = await ResolveBuyerDisplayNameAsync(creature, player).ConfigureAwait(false);
        int goldOnHand = await GetGoldOnHandAsync(creature).ConfigureAwait(false);

        List<PlayerStallProductView> views = BuildProductViews(stall, products);

        PlayerStallSummary summary = new(
            stall.Id,
            stallName,
            null,
            stall.SettlementTag,
            notice);

        PlayerStallBuyerContext context = new(persona, buyerName, goldOnHand);

        return new PlayerStallBuyerSnapshot(summary, context, views);
    }

    private static string BuildStallName(PlayerStall stall)
    {
        if (!string.IsNullOrWhiteSpace(stall.OwnerDisplayName))
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}'s Stall", stall.OwnerDisplayName);
        }

        if (!string.IsNullOrWhiteSpace(stall.Tag))
        {
            return stall.Tag;
        }

        return string.Format(CultureInfo.InvariantCulture, "Stall #{0}", stall.Id);
    }

    private static string? BuildStallNotice(PlayerStall stall)
    {
        if (stall.SuspendedUtc.HasValue)
        {
            return "This stall is temporarily closed.";
        }

        return null;
    }

    private static List<PlayerStallProductView> BuildProductViews(PlayerStall stall, IReadOnlyList<StallProduct> products)
    {
        bool stallOpen = IsStallOpen(stall);
        IEnumerable<StallProduct> ordered = products
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name);

        List<PlayerStallProductView> views = new();

        foreach (StallProduct product in ordered)
        {
            bool soldOut = product.Quantity <= 0 || (product.SoldOutUtc.HasValue && product.SoldOutUtc.Value <= DateTime.UtcNow);
            bool purchasable = stallOpen && product.IsActive && !soldOut;

            string name = string.IsNullOrWhiteSpace(product.Name) ? product.ResRef : product.Name;
            string? tooltip = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description;

            views.Add(new PlayerStallProductView(
                product.Id,
                name,
                Math.Max(0, product.Price),
                Math.Max(0, product.Quantity),
                soldOut,
                purchasable,
                tooltip));
        }

        return views;
    }

    private static List<PlayerStallSellerProductView> BuildSellerProductViews(
        IReadOnlyList<StallProduct> products,
        bool canAdjustPrice)
    {
        IEnumerable<StallProduct> ordered = products
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name);

        List<PlayerStallSellerProductView> views = new();

        foreach (StallProduct product in ordered)
        {
            bool soldOut = product.Quantity <= 0 || (product.SoldOutUtc.HasValue && product.SoldOutUtc.Value <= DateTime.UtcNow);

            string displayName = string.IsNullOrWhiteSpace(product.Name) ? product.ResRef : product.Name;
            string? tooltip = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description;

            views.Add(new PlayerStallSellerProductView(
                product.Id,
                displayName,
                Math.Max(0, product.Price),
                Math.Max(0, product.Quantity),
                product.IsActive,
                soldOut,
                product.SortOrder,
                tooltip,
                canAdjustPrice));
        }

        return views;
    }

    private async Task PublishSellerSnapshotsAsync(
        long stallId,
        PlayerStall stall,
        IReadOnlyList<StallProduct> products,
        Guid? excludeSessionId = null)
    {
        List<(PlayerStallSellerEventCallbacks Callbacks, PersonaId Persona)> targets = CollectSellerCallbacks(stallId, excludeSessionId);

        foreach ((PlayerStallSellerEventCallbacks callbacks, PersonaId persona) in targets)
        {
            PlayerStallSellerSnapshot snapshot = await BuildSellerSnapshotAsync(
                stall,
                products,
                persona,
                null).ConfigureAwait(false);

            try
            {
                await callbacks.OnSnapshot(snapshot).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Seller snapshot callback threw an exception.");
            }
        }
    }

    private static void ApplyReclaimedItemMetadata(NwItem item, PlayerStall stall, StallProduct product)
    {
        if (item is not { IsValid: true })
        {
            return;
        }

        string? personaId = !string.IsNullOrWhiteSpace(product.ConsignedByPersonaId)
            ? product.ConsignedByPersonaId
            : stall.OwnerPersonaId;

        if (!string.IsNullOrWhiteSpace(personaId))
        {
            NWScript.SetLocalString(item, PlayerStallItemLocals.ConsignorPersonaId, personaId);
        }

        NWScript.SetLocalString(item, PlayerStallItemLocals.SourceStallId, stall.Id.ToString(CultureInfo.InvariantCulture));
        NWScript.SetLocalString(item, PlayerStallItemLocals.SourceProductId, product.Id.ToString(CultureInfo.InvariantCulture));
    }

    private List<(PlayerStallSellerEventCallbacks Callbacks, PersonaId Persona)> CollectSellerCallbacks(
        long stallId,
        Guid? excludeSessionId)
    {
        List<(PlayerStallSellerEventCallbacks, PersonaId)> results = new();

        lock (_syncRoot)
        {
            if (!_stallSellers.TryGetValue(stallId, out HashSet<Guid>? sessions))
            {
                return results;
            }

            foreach (Guid sessionId in sessions)
            {
                if (excludeSessionId.HasValue && excludeSessionId.Value == sessionId)
                {
                    continue;
                }

                if (_sellerSessions.TryGetValue(sessionId, out SellerSubscription? subscription) && subscription is not null)
                {
                    results.Add((subscription.Callbacks, subscription.Persona));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Forces every seller session to refresh its snapshot for the specified stall.
    /// </summary>
    public async Task BroadcastSellerRefreshAsync(long stallId)
    {
        PlayerStall? stall = _shops.GetShopWithMembers(stallId);
        if (stall is null)
        {
            return;
        }

        IReadOnlyList<StallProduct> products = _shops.ProductsForShop(stallId) ?? new List<StallProduct>();
        await PublishSellerSnapshotsAsync(stallId, stall, products).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a seller snapshot for the specified stall and persona without registering a live session.
    /// </summary>
    public async Task<PlayerStallSellerSnapshot?> BuildSellerSnapshotForAsync(long stallId, PersonaId persona)
    {
        PlayerStall? stall = _shops.GetShopWithMembers(stallId);
        if (stall is null)
        {
            return null;
        }

        List<StallProduct> products = _shops.ProductsForShop(stallId) ?? new List<StallProduct>();
        return await BuildSellerSnapshotAsync(stall, products, persona, null).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a buyer snapshot for the specified stall and persona without registering a live session.
    /// </summary>
    public async Task<PlayerStallBuyerSnapshot?> BuildBuyerSnapshotForAsync(long stallId, PersonaId persona, NwPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        PlayerStall? stall = _shops.GetShopWithMembers(stallId);
        if (stall is null)
        {
            return null;
        }

        NwCreature? creature = await ResolveActiveCreatureAsync(player).ConfigureAwait(false);
        if (creature is null)
        {
            return null;
        }

        List<StallProduct> products = _shops.ProductsForShop(stallId) ?? new List<StallProduct>();
        return await BuildSnapshotAsync(stall, products, persona, player, creature).ConfigureAwait(false);
    }

    private async Task<PlayerStallSellerSnapshot> BuildSellerSnapshotAsync(
        PlayerStall stall,
        IReadOnlyList<StallProduct> products,
        PersonaId persona,
        long? selectedProductId)
    {
        string stallName = BuildStallName(stall);
        string? notice = BuildStallNotice(stall);

        PlayerStallSummary summary = new(
            stall.Id,
            stallName,
            null,
            stall.SettlementTag,
            notice);

        string sellerName = ResolveSellerDisplayName(stall, persona);
        PlayerStallSellerContext context = new(persona, sellerName);

        List<PlayerStallSellerProductView> views = BuildSellerProductViews(products, canAdjustPrice: true);
        IReadOnlyList<PlayerStallSellerInventoryItemView> inventory = await BuildInventoryItemsAsync(persona).ConfigureAwait(false);

        bool rentFromCoinhouse = stall.CoinHouseAccountId.HasValue;
        bool rentToggleVisible = false;
        bool rentToggleEnabled = false;
        string? rentToggleTooltip = null;

        bool holdToggleVisible = false;
        bool holdToggleEnabled = false;
        string holdToggleLabel = "Hold profits in stall escrow";
        string? holdToggleTooltip = null;

        int escrowBalance = Math.Max(0, stall.EscrowBalance);
        bool earningsRowVisible = false;
        bool withdrawEnabled = false;
        bool withdrawAllEnabled = false;
        string? earningsTooltip = null;

        string personaId = persona.ToString();

        bool isOwner = !string.IsNullOrWhiteSpace(stall.OwnerPersonaId) &&
                       string.Equals(stall.OwnerPersonaId, personaId, StringComparison.OrdinalIgnoreCase);

        bool canCollect = isOwner;
        bool canConfigure = isOwner;

        if (!isOwner && stall.Members is not null && stall.Members.Count > 0)
        {
            foreach (PlayerStallMember member in stall.Members.Where(m => m is not null))
            {
                if (member.RevokedUtc.HasValue)
                {
                    continue;
                }

                if (!string.Equals(member.PersonaId, personaId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                canCollect = member.CanCollectEarnings;
                canConfigure = member.CanConfigureSettings;
                break;
            }
        }

        bool hasCoinhouseLink = TryResolveCoinhouseTag(stall, out CoinhouseTag coinhouseTag);
        bool hasCoinhouseAccount = false;

        if (hasCoinhouseLink)
        {
            Guid accountId = PersonaAccountId.ForCoinhouse(persona, coinhouseTag);
            hasCoinhouseAccount = await _coinhouses
                .GetAccountForAsync(accountId, CancellationToken.None)
                .ConfigureAwait(false) is not null;
        }

        if (isOwner)
        {
            if (hasCoinhouseLink)
            {
                rentToggleVisible = true;
                rentToggleEnabled = hasCoinhouseAccount;
                rentToggleTooltip = hasCoinhouseAccount
                    ? rentFromCoinhouse
                        ? "Rent is currently charged to your coinhouse account."
                        : "Enable to charge rent to your coinhouse account instead of stall profits."
                    : "Open or join a coinhouse account in this settlement to enable automatic rent payments.";
            }
            else if (rentFromCoinhouse)
            {
                rentToggleVisible = true;
                rentToggleEnabled = false;
                rentToggleTooltip = "This stall is not linked to a coinhouse; rent will continue to use stall earnings.";
            }
        }
        else if (rentFromCoinhouse)
        {
            rentToggleVisible = true;
            rentToggleEnabled = false;
            rentToggleTooltip = "Rent payments are charged to the owner's coinhouse account.";
        }

        bool holdEarnings = stall.HoldEarningsInStall;

        holdToggleVisible = canConfigure && (hasCoinhouseLink || rentFromCoinhouse);
        if (holdToggleVisible)
        {
            if (!hasCoinhouseLink && rentFromCoinhouse)
            {
                holdToggleEnabled = false;
                holdToggleTooltip = "This stall is not linked to a coinhouse; profits will remain in escrow.";
            }
            else
            {
                holdToggleEnabled = hasCoinhouseAccount;
                holdToggleTooltip = holdToggleEnabled
                    ? holdEarnings
                        ? "Profits remain in stall escrow until you withdraw them."
                        : "Profits will deposit to your coinhouse account automatically."
                    : "Link your coinhouse account in this settlement to change this option.";
            }
        }

        earningsRowVisible = canCollect;
        withdrawEnabled = canCollect && escrowBalance > 0;
        withdrawAllEnabled = withdrawEnabled;

        if (!canCollect)
        {
            earningsTooltip = "You do not have permission to withdraw stall earnings.";
        }
        else if (escrowBalance <= 0)
        {
            earningsTooltip = "No earnings are currently available to withdraw.";
        }

        return new PlayerStallSellerSnapshot(
            summary,
            context,
            views,
            inventory,
            null,
            null,
            false,
            selectedProductId,
            rentFromCoinhouse,
            rentToggleVisible,
            rentToggleEnabled,
            rentToggleTooltip,
            holdEarnings,
            holdToggleVisible,
            holdToggleEnabled,
            holdToggleTooltip,
            holdToggleLabel,
            escrowBalance,
            earningsRowVisible,
            withdrawEnabled,
            withdrawAllEnabled,
            earningsTooltip);
    }

    private bool TryResolveCoinhouseTag(PlayerStall stall, out CoinhouseTag tag)
    {
        tag = default;

        if (string.IsNullOrWhiteSpace(stall.SettlementTag))
        {
            return false;
        }

        string raw = stall.SettlementTag.Trim();

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int settlementId) && settlementId > 0)
        {
            try
            {
                SettlementId settlement = SettlementId.Parse(settlementId);
                CoinHouse? coinhouse = _coinhouses.GetSettlementCoinhouse(settlement);
                if (coinhouse is not null && !string.IsNullOrWhiteSpace(coinhouse.Tag))
                {
                    tag = CoinhouseTag.Parse(coinhouse.Tag);
                    return true;
                }

                Log.Debug("No coinhouse registered for settlement {SettlementId} while resolving stall {StallId}.", settlementId, stall.Id);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to resolve coinhouse for settlement {SettlementId} on stall {StallId}.", settlementId, stall.Id);
                return false;
            }
        }

        try
        {
            tag = CoinhouseTag.Parse(raw);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Log.Warn(ex, "Invalid coinhouse tag '{Tag}' for stall {StallId}.", raw, stall.Id);
            return false;
        }
    }

    private async Task<IReadOnlyList<PlayerStallSellerInventoryItemView>> BuildInventoryItemsAsync(PersonaId persona)
    {
        List<PlayerStallSellerInventoryItemView> items = new();

        if (!TryResolvePersonaGuid(persona, out Guid personaGuid))
        {
            return items;
        }

        if (!_characters.TryGetPlayer(personaGuid, out NwPlayer? player) || player is null)
        {
            return items;
        }

        NwCreature? creature = await ResolveActiveCreatureAsync(player).ConfigureAwait(false);
        if (creature is null)
        {
            return items;
        }

        await NwTask.SwitchToMainThread();

        foreach (NwItem item in creature.Inventory.Items)
        {
            if (!PlayerStallInventoryPolicy.IsItemAllowed(item))
            {
                continue;
            }

            string objectId = NWScript.ObjectToString(item);
            string displayName = string.IsNullOrWhiteSpace(item.Name) ? item.ResRef : item.Name.Trim();
            string resRef = item.ResRef;
            int quantity = Math.Max(item.StackSize, 1);
            bool stackable = quantity > 1;
            string? description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
            int? baseItemType = item.BaseItem is null ? null : (int)item.BaseItem.ItemType;

            items.Add(new PlayerStallSellerInventoryItemView(
                objectId,
                displayName,
                resRef,
                quantity,
                stackable,
                description,
                baseItemType));
        }

        return items;
    }

    private static string ResolveSellerDisplayName(PlayerStall stall, PersonaId persona)
    {
        string personaId = persona.ToString();

        if (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId) &&
            string.Equals(stall.OwnerPersonaId, personaId, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(stall.OwnerDisplayName))
        {
            return stall.OwnerDisplayName!;
        }

        if (stall.Members is not null)
        {
            PlayerStallMember? match = stall.Members
                .FirstOrDefault(member =>
                    member.RevokedUtc is null &&
                    string.Equals(member.PersonaId, personaId, StringComparison.OrdinalIgnoreCase));

            if (match is not null && !string.IsNullOrWhiteSpace(match.DisplayName))
            {
                return match.DisplayName;
            }
        }

        return personaId;
    }

    private static string BuildSuccessMessage(StallProduct product, int totalPrice)
    {
        string name = string.IsNullOrWhiteSpace(product.Name) ? product.ResRef : product.Name;

        if (totalPrice <= 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "You received {0}.", name);
        }

        return string.Format(CultureInfo.InvariantCulture, "You purchased {0} for {1:n0} gp.", name, totalPrice);
    }

    private sealed record BuyerSubscription(long StallId, PersonaId Persona, PlayerStallBuyerEventCallbacks Callbacks);

    private sealed record SellerSubscription(long StallId, PersonaId Persona, PlayerStallSellerEventCallbacks Callbacks);
}
