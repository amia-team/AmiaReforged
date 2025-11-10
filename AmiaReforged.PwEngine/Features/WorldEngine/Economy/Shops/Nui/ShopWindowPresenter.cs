using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Logging;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Nui;

public sealed class ShopWindowPresenter : ScryPresenter<ShopWindowView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly NpcShop _shop;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly List<NwItem> _inventoryItems = new();
    private readonly List<NpcShopProduct?> _inventoryProducts = new();
    private readonly List<int> _inventoryBuyPrices = new();
    private readonly List<NwItem> _identifyTargets = new();
    private int _identifyAllCost;
    private bool _shopkeeperResolved;
    private NwCreature? _shopkeeper;
    private bool _listeningForUpdates;

    private const int IdentifyCostPerItem = 100;

    public ShopWindowPresenter(ShopWindowView view, NwPlayer player, NpcShop shop)
    {
        View = view;
        _player = player;
        _shop = shop;
    }

    [Inject] private Lazy<IShopItemBlacklist> ItemBlacklist { get; init; } = null!;
    [Inject] private Lazy<INpcShopItemFactory> ItemFactory { get; init; } = null!;
    [Inject] private Lazy<IShopPriceCalculator> PriceCalculator { get; init; } = null!;
    [Inject] private Lazy<INpcShopRepository> ShopRepository { get; init; } = null!;

    public override ShopWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        string title = string.IsNullOrWhiteSpace(_shop.DisplayName)
            ? "Merchant Ledger"
            : _shop.DisplayName;

        _window = new NuiWindow(View.RootLayout(), title)
        {
            Geometry = new NuiRect(60f, 80f, 6230f, 640f),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The merchant ledger could not be configured. Please alert a DM if this persists.",
                ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage(
                message: "Unable to open the merchant ledger right now.",
                ColorConstants.Orange);
            return;
        }

        try
        {
            UpdateView();
            RegisterForRepositoryUpdates();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize NPC shop window for player {PlayerName} targeting shop {ShopTag}.",
                _player.PlayerName, _shop.Tag);
            _player.SendServerMessage(
                message: "The shop window encountered an error while loading.",
                ColorConstants.Orange);
            RaiseCloseEvent();
            Close();
        }
    }

    public override void UpdateView()
    {
        NwCreature? buyer = ResolveActiveBuyer();

        List<bool> productPurchasable;
        List<string> productTooltips;
        List<string> productEntries = BuildProductEntries(buyer, out productPurchasable, out productTooltips);
        List<bool> inventorySellable;
        List<string> inventoryItemIds;
        List<string> inventoryEntries = BuildInventoryEntries(buyer, out inventoryItemIds, out inventorySellable);

        Token().SetBindValue(View.StoreTitle, BuildTitleText());
        Token().SetBindValue(View.StoreDescription, BuildDescriptionText());

        Token().SetBindValues(View.ProductEntries, productEntries);
        Token().SetBindValues(View.ProductTooltips, productTooltips);
        Token().SetBindValues(View.ProductPurchasable, productPurchasable);
        Token().SetBindValue(View.ProductCount, productEntries.Count);

        Token().SetBindValues(View.InventoryEntries, inventoryEntries);
        Token().SetBindValues(View.InventoryItemIds, inventoryItemIds);
        Token().SetBindValues(View.InventorySellable, inventorySellable);
        Token().SetBindValue(View.InventoryCount, inventoryEntries.Count);
        Token().SetBindValue(View.IdentifyButtonLabel, BuildIdentifyButtonLabel());
        Token().SetBindValue(View.IdentifyButtonEnabled, _identifyTargets.Count > 0);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click)
        {
            return;
        }

        if (eventData.ElementId == View.CloseButton.Id)
        {
            RaiseCloseEvent();
            Close();
        }

        if (eventData.ElementId == View.BuyButton.Id)
        {
            int rowIndex = eventData.ArrayIndex;
            if (rowIndex < 0)
            {
                return;
            }

            _ = HandlePurchaseAsync(rowIndex);
            return;
        }

        if (eventData.ElementId == View.IdentifyAllButton.Id)
        {
            _ = HandleIdentifyAllAsync();
            return;
        }

        if (eventData.ElementId == View.SellButton.Id)
        {
            int rowIndex = eventData.ArrayIndex;
            if (rowIndex < 0)
            {
                return;
            }

            _ = HandleSellAsync(rowIndex);
        }
    }

    public override void Close()
    {
        UnregisterFromRepositoryUpdates();

        try
        {
            _token.Close();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "NPC shop token close threw an exception for player {PlayerName}.", _player.PlayerName);
        }
    }

    private string BuildTitleText()
    {
        string displayName = string.IsNullOrWhiteSpace(_shop.DisplayName) ? "Merchant" : _shop.DisplayName;
        return displayName;
    }

    private string BuildDescriptionText()
    {
        if (!string.IsNullOrWhiteSpace(_shop.Description))
        {
            return _shop.Description;
        }

        DateTime nextRestock = _shop.NextRestockUtc;
        if (nextRestock == default)
        {
            return "Restock schedule is being prepared.";
        }

        TimeSpan remaining = nextRestock - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return "Restock is expected imminently.";
        }

        string formatted = remaining.TotalHours >= 1
            ? string.Format(CultureInfo.InvariantCulture, "Restock in {0:0.#} hours.", remaining.TotalHours)
            : string.Format(CultureInfo.InvariantCulture, "Restock in {0:0.#} minutes.", remaining.TotalMinutes);

        return formatted;
    }

    private string BuildIdentifyButtonLabel()
    {
        if (_identifyTargets.Count == 0)
        {
            return "Identify All (100 gp each)";
        }

        string costLabel = string.Format(CultureInfo.InvariantCulture, "{0:N0} gp", _identifyAllCost);
        return $"Identify All ({costLabel})";
    }

    private List<string> BuildProductEntries(NwCreature? buyer, out List<bool> purchasable, out List<string> tooltips)
    {
        purchasable = new List<bool>();
        tooltips = new List<string>();

        if (_shop.Products.Count == 0)
        {
            purchasable.Add(false);
            tooltips.Add("This merchant has nothing to offer right now.");
            return new List<string> { "This merchant has nothing to sell." };
        }

        List<string> entries = new(_shop.Products.Count);

        foreach (NpcShopProduct product in _shop.Products)
        {
            string displayName = product.DisplayName;
            string stockLabel = product.IsOutOfStock
                ? "Sold out"
                : string.Format(CultureInfo.InvariantCulture, "{0}/{1} on hand", product.CurrentStock,
                    product.MaxStock);

            int salePrice = CalculateSalePrice(product, buyer);

            string priceLabel = salePrice == 0
                ? "Complimentary"
                : string.Format(CultureInfo.InvariantCulture, "{0:N0} gp", salePrice);

            entries.Add($"{displayName} — {priceLabel} ({stockLabel})");
            purchasable.Add(!product.IsOutOfStock);

            string description = string.IsNullOrWhiteSpace(product.Description)
                ? string.Empty
                : product.Description.Trim();

            string tooltip = string.IsNullOrEmpty(description)
                ? $"ResRef: {product.ResRef}"
                : $"{description}\n\nResRef: {product.ResRef}";

            tooltips.Add(tooltip);
        }

        return entries;
    }

    private async Task HandlePurchaseAsync(int productIndex)
    {
        if (productIndex < 0 || productIndex >= _shop.Products.Count)
        {
            return;
        }

        NpcShopProduct product = _shop.Products[productIndex];

        if (product.IsOutOfStock)
        {
            await NotifyAsync("That item is sold out.", ColorConstants.Orange, refresh: true);
            return;
        }

        if (!ShopRepository.Value.TryConsumeProduct(_shop.Tag, product.ResRef, 1, out ConsignedItemData? consignedItem))
        {
            await NotifyAsync("Another customer just claimed the last one.", ColorConstants.Orange, refresh: true);
            return;
        }

        NwCreature? buyer = ResolveActiveBuyer();
        if (buyer is null)
        {
            ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1, consignedItem);
            await NotifyAsync("You must be possessing your character to make a purchase.", ColorConstants.Orange);
            return;
        }

        int salePrice = CalculateSalePrice(product, buyer);
        bool paymentCaptured = false;

        if (salePrice > 0)
        {
            bool paymentTaken = await TryWithdrawGoldAsync(buyer, salePrice);
            if (!paymentTaken)
            {
                ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1, consignedItem);
                await NotifyAsync("You cannot afford that purchase.", ColorConstants.Orange, refresh: true);
                return;
            }

            paymentCaptured = true;
        }

        try
        {
            NwItem? item = await ItemFactory.Value.CreateForInventoryAsync(buyer, product, consignedItem);
            if (item is null)
            {
                if (paymentCaptured)
                {
                    await RefundGoldAsync(buyer, salePrice);
                    paymentCaptured = false;
                }

                ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1, consignedItem);
                await NotifyAsync("The merchant cannot produce that item right now.", ColorConstants.Orange,
                    refresh: true);
                return;
            }

            string itemName = string.IsNullOrWhiteSpace(item.Name) ? product.DisplayName : item.Name;
            string message = BuildPurchaseMessage(itemName, salePrice);
            await NotifyAsync(message, ColorConstants.Lime, refresh: true);
        }
        catch (Exception ex)
        {
            if (paymentCaptured)
            {
                await RefundGoldAsync(buyer, salePrice);
            }

            ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1, consignedItem);
            Log.Error(ex,
                "Failed to fulfill item purchase for player {PlayerName} in shop {ShopTag} (item {ResRef}).",
                _player.PlayerName,
                _shop.Tag,
                product.ResRef);

            await NotifyAsync("The merchant fumbles with that order; please try again shortly.",
                ColorConstants.Orange,
                refresh: true);
        }
    }

    private async Task HandleSellAsync(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _inventoryItems.Count)
        {
            return;
        }

        NwItem item = _inventoryItems[inventoryIndex];
        if (item is null || !item.IsValid)
        {
            await NotifyAsync("That item is no longer available.", ColorConstants.Orange, refresh: true);
            return;
        }

        NwCreature? seller = ResolveActiveBuyer();
        if (seller is null)
        {
            await NotifyAsync("You are not possessing a creature.", ColorConstants.Orange);
            return;
        }

        if (item.Possessor != seller)
        {
            await NotifyAsync("You must be holding the item to sell it.", ColorConstants.Orange, refresh: true);
            return;
        }

        NpcShopProduct? product = inventoryIndex < _inventoryProducts.Count ? _inventoryProducts[inventoryIndex] : null;

        if (product is null)
        {
            HashSet<int> acceptedTypes = BuildAcceptedBaseItemSet();
            if (!TryGetSellProduct(item, acceptedTypes, out product) || product is null)
            {
                await NotifyAsync("This merchant is not interested in that item.", ColorConstants.Orange,
                    refresh: true);
                return;
            }
        }

        int pricePerItem = (inventoryIndex < _inventoryBuyPrices.Count) ? _inventoryBuyPrices[inventoryIndex] : 0;
        if (pricePerItem <= 0)
        {
            await NotifyAsync("The merchant cannot offer coin for that item right now.", ColorConstants.Orange,
                refresh: true);
            return;
        }

        int quantity = Math.Max(item.StackSize, 1);
        int payout = pricePerItem * quantity;

        ConsignedItemData consignedItem = BuildConsignedItemData(item);
        ShopProductRecord listing = BuildConsignmentProductRecord(item, product, consignedItem.Quantity);

        if (!ShopRepository.Value.TryStorePlayerProduct(_shop.Tag, listing, consignedItem))
        {
            Log.Warn("Failed to persist consigned item {ResRef} for shop {Tag}.", listing.ResRef, _shop.Tag);
            await NotifyAsync("The merchant cannot accept that item right now.", ColorConstants.Orange, refresh: false);
            return;
        }

        string itemName = string.IsNullOrWhiteSpace(item.Name) ? product.DisplayName : item.Name;

        await NwTask.SwitchToMainThread();
        if (item.IsValid)
        {
            item.Destroy();
        }

        if (payout > 0)
        {
            await RefundGoldAsync(seller, payout);
        }

        string message = payout > 0
            ? string.Format(CultureInfo.InvariantCulture, "You sold {0} for {1:N0} gp.", itemName, payout)
            : string.Format(CultureInfo.InvariantCulture, "You consign {0} with the merchant.", itemName);

        await NotifyAsync(message, ColorConstants.Lime, refresh: true);
    }

    private async Task HandleIdentifyAllAsync()
    {
        NwCreature? buyer = ResolveActiveBuyer();
        if (buyer is null)
        {
            await NotifyAsync("You are not possessing a creature.", ColorConstants.Orange, refresh: true);
            return;
        }

        List<NwItem> candidates = GatherIdentifyCandidates(buyer);
        if (candidates.Count == 0)
        {
            await NotifyAsync("You have nothing that needs identification.", ColorConstants.Orange, refresh: true);
            return;
        }

        int totalCost = candidates.Count * IdentifyCostPerItem;
        bool paymentCaptured = false;

        if (totalCost > 0)
        {
            bool paymentTaken = await TryWithdrawGoldAsync(buyer, totalCost);
            if (!paymentTaken)
            {
                string costLabel = string.Format(CultureInfo.InvariantCulture, "{0:N0} gp", totalCost);
                await NotifyAsync($"You need {costLabel} to identify those items.", ColorConstants.Orange,
                    refresh: true);
                return;
            }

            paymentCaptured = true;
        }

        try
        {
            await IdentifyItemsAsync(candidates);

            string costLabel = string.Format(CultureInfo.InvariantCulture, "{0:N0} gp", totalCost);
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "The merchant identified {0} item{1} for {2}.",
                candidates.Count,
                candidates.Count == 1 ? string.Empty : "s",
                costLabel);

            await NotifyAsync(message, ColorConstants.Lime, refresh: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to identify items for player {PlayerName} in shop {ShopTag}.",
                _player.PlayerName, _shop.Tag);

            if (paymentCaptured && totalCost > 0)
            {
                await RefundGoldAsync(buyer, totalCost);
            }

            await NotifyAsync("The merchant was unable to complete the identification.", ColorConstants.Orange,
                refresh: true);
        }
    }

    private static string BuildPurchaseMessage(string itemName, int price)
    {
        if (price <= 0)
        {
            return $"You received {itemName}.";
        }

        string priceLabel = string.Format(CultureInfo.InvariantCulture, "{0:N0} gp", price);
        return $"You purchased {itemName} for {priceLabel}.";
    }

    private async Task NotifyAsync(string message, Color color, bool refresh = false)
    {
        await NwTask.SwitchToMainThread();

        if (_player.IsValid)
        {
            _player.SendServerMessage(message, color);
        }

        if (!refresh)
        {
            return;
        }

        try
        {
            UpdateView();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Refreshing NPC shop view failed for player {PlayerName}.", _player.PlayerName);
        }
    }

    private void RegisterForRepositoryUpdates()
    {
        if (_listeningForUpdates)
        {
            return;
        }

        ShopRepository.Value.ShopChanged += HandleShopChanged;
        Closing += HandleClosing;
        _listeningForUpdates = true;
    }

    private void UnregisterFromRepositoryUpdates()
    {
        if (!_listeningForUpdates)
        {
            return;
        }

        ShopRepository.Value.ShopChanged -= HandleShopChanged;
        Closing -= HandleClosing;
        _listeningForUpdates = false;
    }

    private void HandleShopChanged(object? sender, NpcShopChangedEventArgs args)
    {
        if (!string.Equals(args.Shop.Tag, _shop.Tag, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        switch (args.ChangeKind)
        {
            case NpcShopChangeKind.StockChanged:
            case NpcShopChangeKind.ProductsChanged:
            case NpcShopChangeKind.MetadataChanged:
                _ = RefreshAsync();
                break;
        }
    }

    private void HandleClosing(IScryPresenter sender, EventArgs eventArgs)
    {
        UnregisterFromRepositoryUpdates();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await NwTask.SwitchToMainThread();

            if (!_player.IsValid)
            {
                return;
            }

            if (_token.Token == 0)
            {
                return;
            }

            UpdateView();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to refresh NPC shop view for player {PlayerName}.", _player.PlayerName);
        }
    }

    private List<string> BuildInventoryEntries(
        NwCreature? creature,
        out List<string> itemIds,
        out List<bool> sellable)
    {
        itemIds = new List<string>();
        sellable = new List<bool>();
        _inventoryItems.Clear();
        _inventoryProducts.Clear();
        _inventoryBuyPrices.Clear();
        _identifyTargets.Clear();
        _identifyAllCost = 0;

        if (creature == null)
        {
            return new List<string> { "You are not possessing a creature." };
        }

        List<string> entries = new();
        HashSet<int> acceptedTypes = BuildAcceptedBaseItemSet();

        foreach (NwItem item in creature.Inventory.Items)
        {
            Log.Info(item.BaseItem.ItemType);
            _inventoryItems.Add(item);

            string itemName = string.IsNullOrWhiteSpace(item.Name) ? item.ResRef : item.Name;
            int stack = item.StackSize;
            string stackInfo = stack > 1 ? $" x{stack}" : string.Empty;

            if (NeedsIdentification(item))
            {
                _identifyTargets.Add(item);
            }

            if (TryGetSellProduct(item, acceptedTypes, out NpcShopProduct? product) && product is not null)
            {
                int buyback = CalculateBuybackPrice(product, creature!);
                int total = buyback * Math.Max(stack, 1);

                entries.Add($"{itemName}{stackInfo} — Sell {total} gp");
                sellable.Add(true);
                _inventoryProducts.Add(product);
                _inventoryBuyPrices.Add(buyback);
            }
            else
            {
                entries.Add($"{itemName}{stackInfo}");
                sellable.Add(false);
                _inventoryProducts.Add(null);
                _inventoryBuyPrices.Add(0);
            }

            itemIds.Add(item.ObjectId.ToString(CultureInfo.InvariantCulture));
        }

        if (entries.Count == 0)
        {
            entries.Add("No items in your inventory may be sold here.");
            itemIds.Add(string.Empty);
            sellable.Add(false);
        }

        _identifyAllCost = _identifyTargets.Count * IdentifyCostPerItem;

        return entries;
    }

    private NwCreature? ResolveActiveBuyer()
    {
        NwCreature? creature = _player.ControlledCreature ?? _player.LoginCreature;
        if (creature is null || !creature.IsValid)
        {
            return null;
        }

        return creature;
    }

    private int CalculateSalePrice(NpcShopProduct product, NwCreature? buyer)
    {
        int basePrice;

        try
        {
            basePrice = Math.Max(0, PriceCalculator.Value.CalculatePrice(_shop, product, buyer));
        }
        catch (Exception ex)
        {
            Log.Warn(ex,
                "Shop price calculation failed for product {ProductResRef} in shop {ShopTag}.",
                product.ResRef,
                _shop.Tag);
            basePrice = Math.Max(0, product.Price);
        }

        return ApplyAppraiseForPurchase(basePrice, buyer);
    }

    private int CalculateBuybackPrice(NpcShopProduct product, NwCreature seller)
    {
        int basePrice;

        try
        {
            basePrice = Math.Max(0, PriceCalculator.Value.CalculatePrice(_shop, product, seller));
        }
        catch (Exception ex)
        {
            Log.Warn(ex,
                "Shop buyback calculation failed for product {ProductResRef} in shop {ShopTag}.",
                product.ResRef,
                _shop.Tag);
            basePrice = Math.Max(0, product.Price);
        }

        int negotiated = ApplyAppraiseForSell(basePrice, seller);
        if (negotiated <= 0)
        {
            return 0;
        }

        int buyback = negotiated / 2;
        return Math.Max(1, buyback);
    }

    private static async Task<bool> TryWithdrawGoldAsync(NwCreature buyer, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        await NwTask.SwitchToMainThread();

        if (!buyer.IsValid)
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

        if (!buyer.IsValid)
        {
            return;
        }

        buyer.Gold += (uint)amount;
    }

    private HashSet<int> BuildAcceptedBaseItemSet()
    {
        return new HashSet<int>(_shop.AcceptedBaseItemTypes);
    }

    private bool TryGetSellProduct(NwItem item, HashSet<int> acceptedTypes, out NpcShopProduct? product)
    {
        product = null;

        if (!item.IsValid || item.PlotFlag)
        {
            return false;
        }

        Log.Info($"{item.Name} is valid? {item.IsValid}, plot? {item.PlotFlag}");

        if (item.StackSize < 0)
        {
            return false;
        }

        if (acceptedTypes.Count == 0)
        {
            Log.Info("This shop isn't accepting anything");
            return false;
        }

        string itemResRef = item.ResRef;
        if (ItemBlacklist.Value.IsBlacklisted(itemResRef))
        {
            return false;
        }


        int baseTypeId = (int)item.BaseItem.ItemType;

        if (!acceptedTypes.Contains(baseTypeId))
        {
            Log.Info($"Base type was {baseTypeId} and the shop does not accept it");
            return false;
        }

        product = FindProductForItem(item, baseTypeId);
        return product is not null;
    }

    private NpcShopProduct? FindProductForItem(NwItem item, int baseTypeId)
    {
        NpcShopProduct? direct = _shop.FindProduct(item.ResRef);
        if (direct is not null)
        {
            return direct;
        }

        IReadOnlyList<NpcShopProduct> baseMatches = _shop.FindProductsByBaseItemType(baseTypeId);
        if (baseMatches.Count > 0)
        {
            return baseMatches.OrderBy(p => p.SortOrder).First();
        }

        return CreateAdHocProduct(item, baseTypeId);
    }

    private NpcShopProduct? CreateAdHocProduct(NwItem item, int baseTypeId)
    {
        string resRef = string.IsNullOrWhiteSpace(item.ResRef)
            ? $"player_item_{baseTypeId}"
            : item.ResRef.Trim();

        string displayName = string.IsNullOrWhiteSpace(item.Name) ? resRef : item.Name.Trim();
        string? description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
        int stack = Math.Max(item.StackSize, 1);
        int price;

        try
        {
            price = NWScript.GetGoldPieceValue(item);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to read gold value for item {ResRef}.", resRef);
            price = 0;
        }

        price = Math.Max(price, 1);

        try
        {
            return new NpcShopProduct(
                id: -1,
                resRef: resRef,
                displayName: displayName,
                description: description,
                price: price,
                currentStock: stack,
                maxStock: stack,
                restockAmount: 0,
                isPlayerManaged: true,
                sortOrder: 0,
                baseItemType: baseTypeId);
        }
        catch (ArgumentException ex)
        {
            Log.Debug(ex, "Failed to map item {ResRef} to shop product.", resRef);
            return null;
        }
    }

    private static List<NwItem> GatherIdentifyCandidates(NwCreature creature)
    {
        List<NwItem> items = new();

        foreach (NwItem item in creature.Inventory.Items)
        {
            if (NeedsIdentification(item))
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static bool NeedsIdentification(NwItem item)
    {
        if (item is null || !item.IsValid)
        {
            return false;
        }

        try
        {
            return !item.Identified;
        }
        catch
        {
            return false;
        }
    }

    private static async Task IdentifyItemsAsync(IEnumerable<NwItem> items)
    {
        await NwTask.SwitchToMainThread();

        foreach (NwItem item in items)
        {
            if (item is null || !item.IsValid)
            {
                continue;
            }

            try
            {
                item.Identified = true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to identify item {ItemResRef}.", item.ResRef);
            }
        }
    }

    private int ApplyAppraiseForPurchase(int price, NwCreature? buyer)
    {
        if (price <= 0)
        {
            return 0;
        }

        int buyerSkill = GetAppraiseSkill(buyer);
        int merchantSkill = GetAppraiseSkill(ResolveShopkeeper());
        int delta = Math.Clamp(buyerSkill - merchantSkill, -15, 15);

        if (delta == 0)
        {
            return price;
        }

        decimal multiplier = 1m - (delta / 100m);
        return ScalePrice(price, multiplier);
    }

    private int ApplyAppraiseForSell(int price, NwCreature seller)
    {
        if (price <= 0)
        {
            return 0;
        }

        int sellerSkill = GetAppraiseSkill(seller);
        int merchantSkill = GetAppraiseSkill(ResolveShopkeeper());
        int delta = Math.Clamp(sellerSkill - merchantSkill, -15, 15);

        if (delta == 0)
        {
            return price;
        }

        decimal multiplier = 1m + (delta / 100m);
        return ScalePrice(price, multiplier);
    }

    private static int GetAppraiseSkill(NwCreature? creature)
    {
        if (creature is null || !creature.IsValid)
        {
            return 0;
        }

        try
        {
            return creature.GetSkillRank(Skill.Appraise!);
        }
        catch
        {
            return 0;
        }
    }

    private NwCreature? ResolveShopkeeper()
    {
        if (_shopkeeperResolved)
        {
            if (_shopkeeper is { IsValid: false })
            {
                _shopkeeper = null;
            }

            return _shopkeeper;
        }

        _shopkeeperResolved = true;

        if (string.IsNullOrWhiteSpace(_shop.ShopkeeperTag))
        {
            return null;
        }

        _shopkeeper = NwObject.FindObjectsWithTag<NwCreature>(_shop.ShopkeeperTag)
            .FirstOrDefault(creature => creature.IsValid);

        return _shopkeeper;
    }

    private ConsignedItemData BuildConsignedItemData(NwItem item)
    {
        Json serialized = NWScript.ObjectToJson(item);
        string payload = serialized.Dump();
        byte[] itemData = Encoding.UTF8.GetBytes(payload);
        int quantity = Math.Max(item.StackSize, 1);
        string? name = string.IsNullOrWhiteSpace(item.Name) ? item.ResRef : item.Name.Trim();

        return new ConsignedItemData(itemData, quantity, name, item.ResRef);
    }

    private ShopProductRecord BuildConsignmentProductRecord(NwItem item, NpcShopProduct template, int quantity)
    {
        string resRef = GenerateConsignmentResRef(template.ResRef);
        string displayName = string.IsNullOrWhiteSpace(item.Name)
            ? template.DisplayName
            : item.Name.Trim();

        if (displayName.Length > 255)
        {
            displayName = displayName[..255];
        }

        string? description = string.IsNullOrWhiteSpace(item.Description)
            ? template.Description
            : item.Description.Trim();

        int price = Math.Max(template.Price, 1);
        int? baseItemType = template.BaseItemType;

        if (item.BaseItem is { } baseItem)
        {
            baseItemType = (int)baseItem.ItemType;
        }

        int sortOrder = GetNextSortOrder();

        return new ShopProductRecord
        {
            ResRef = resRef,
            DisplayName = displayName,
            Description = description,
            Price = price,
            CurrentStock = quantity,
            MaxStock = quantity,
            RestockAmount = 0,
            BaseItemType = baseItemType,
            IsPlayerManaged = true,
            SortOrder = sortOrder,
            LocalVariablesJson = null,
            AppearanceJson = null
        };
    }

    private int GetNextSortOrder()
    {
        return _shop.Products.Count == 0
            ? 0
            : _shop.Products.Max(p => p.SortOrder) + 1;
    }

    private static string GenerateConsignmentResRef(string templateResRef)
    {
        string baseResRef = string.IsNullOrWhiteSpace(templateResRef)
            ? "consign"
            : templateResRef.Trim();

        string suffix = RandomNumberGenerator.GetHexString(8).ToLowerInvariant();
        int maxPrefix = Math.Max(1, 64 - suffix.Length - 1);

        if (baseResRef.Length > maxPrefix)
        {
            baseResRef = baseResRef[..maxPrefix];
        }

        string candidate = $"{baseResRef}_{suffix}";
        return candidate.Length <= 64 ? candidate : candidate[..64];
    }

    private static int ScalePrice(int price, decimal multiplier)
    {
        decimal scaled = price * multiplier;
        int rounded = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
        return Math.Max(0, rounded);
    }
}
