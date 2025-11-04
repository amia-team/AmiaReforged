using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
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
public sealed class PlayerStallEventManager
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
            deliveredItem = await TryCreateItemAsync(product, buyerCreature).ConfigureAwait(false);
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

        bool rentFromCoinhouse = stall.CoinHouseAccountId.HasValue;
        bool rentToggleVisible = false;
        bool rentToggleEnabled = false;
        string? rentToggleTooltip = null;

        bool isOwner = !string.IsNullOrWhiteSpace(stall.OwnerPersonaId) &&
                       string.Equals(stall.OwnerPersonaId, persona.ToString(), StringComparison.OrdinalIgnoreCase);

        if (isOwner)
        {
            if (TryResolveCoinhouseTag(stall, out CoinhouseTag coinhouseTag))
            {
                Guid accountId = PersonaAccountId.ForCoinhouse(persona, coinhouseTag);
                bool hasAccount = await _coinhouses.GetAccountForAsync(accountId, CancellationToken.None).ConfigureAwait(false) is not null;

                rentToggleVisible = true;
                rentToggleEnabled = hasAccount;
                rentToggleTooltip = hasAccount
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

        return new PlayerStallSellerSnapshot(
            summary,
            context,
            views,
            null,
            null,
            false,
            selectedProductId,
            rentFromCoinhouse,
            rentToggleVisible,
            rentToggleEnabled,
            rentToggleTooltip);
    }

    private static bool TryResolveCoinhouseTag(PlayerStall stall, out CoinhouseTag tag)
    {
        tag = default;

        if (string.IsNullOrWhiteSpace(stall.SettlementTag))
        {
            return false;
        }

        try
        {
            tag = CoinhouseTag.Parse(stall.SettlementTag);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            return false;
        }
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
