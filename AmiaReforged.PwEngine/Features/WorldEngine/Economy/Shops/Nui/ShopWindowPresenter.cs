using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Nui;

public sealed class ShopWindowPresenter : ScryPresenter<ShopWindowView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly NpcShop _shop;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly List<NwItem> _inventoryItems = new();
    private readonly List<NpcShopProduct?> _inventoryProducts = new();
    private readonly List<int> _inventoryBuyPrices = new();

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
            Geometry = new NuiRect(60f, 80f, 620f, 640f),
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

        if (!ShopRepository.Value.TryConsumeProduct(_shop.Tag, product.ResRef, 1))
        {
            await NotifyAsync("Another customer just claimed the last one.", ColorConstants.Orange, refresh: true);
            return;
        }

        NwCreature? buyer = ResolveActiveBuyer();
        if (buyer is null)
        {
            ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1);
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
                ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1);
                await NotifyAsync("You cannot afford that purchase.", ColorConstants.Orange, refresh: true);
                return;
            }

            paymentCaptured = true;
        }

        try
        {
            NwItem? item = await ItemFactory.Value.CreateForInventoryAsync(buyer, product);
            if (item is null)
            {
                if (paymentCaptured)
                {
                    await RefundGoldAsync(buyer, salePrice);
                    paymentCaptured = false;
                }

                ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1);
                await NotifyAsync("The merchant cannot produce that item right now.", ColorConstants.Orange, refresh: true);
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

            ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, 1);
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

    if (!CanSell(item, out NpcShopProduct? product) || product is null)
        {
            await NotifyAsync("This merchant is not interested in that item.", ColorConstants.Orange, refresh: true);
            return;
        }

        int pricePerItem = (inventoryIndex < _inventoryBuyPrices.Count) ? _inventoryBuyPrices[inventoryIndex] : 0;
        if (pricePerItem <= 0)
        {
            await NotifyAsync("The merchant cannot offer coin for that item right now.", ColorConstants.Orange, refresh: true);
            return;
        }

        int quantity = Math.Max(item.StackSize, 1);
        int payout = pricePerItem * quantity;

        try
        {
            ShopRepository.Value.ReturnProduct(_shop.Tag, product.ResRef, quantity);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to store sold item {ResRef} for shop {Tag}.", product.ResRef, _shop.Tag);
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

        if (creature == null)
        {
            return new List<string> { "You are not possessing a creature." };
        }

        List<string> entries = new();

        foreach (NwItem item in creature.Inventory.Items)
        {
            _inventoryItems.Add(item);

            string itemName = string.IsNullOrWhiteSpace(item.Name) ? item.ResRef : item.Name;
            int stack = item.StackSize;
            string stackInfo = stack > 1 ? $" x{stack}" : string.Empty;

            if (CanSell(item, out NpcShopProduct? product))
            {
                int salePrice = CalculateSalePrice(product, creature);
                int buyback = CalculateBuybackPrice(salePrice);
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
        try
        {
            return Math.Max(0, PriceCalculator.Value.CalculatePrice(_shop, product, buyer));
        }
        catch (Exception ex)
        {
            Log.Warn(ex,
                "Shop price calculation failed for product {ProductResRef} in shop {ShopTag}.",
                product.ResRef,
                _shop.Tag);
            return Math.Max(0, product.Price);
        }
    }

    private static int CalculateBuybackPrice(int salePrice)
    {
        if (salePrice <= 0)
        {
            return 0;
        }

        int buyback = salePrice / 2;
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

    private bool CanSell(NwItem item, out NpcShopProduct? matchedProduct)
    {
        matchedProduct = null;

        if (!item.IsValid)
        {
            return false;
        }

        if (item.PlotFlag)
        {
            return false;
        }

        if (item.StackSize <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.ResRef))
        {
            return false;
        }

        if (item.BaseItem is null)
        {
            return false;
        }

        try
        {
            if (ItemBlacklist.Value.IsBlacklisted(item.ResRef))
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Blacklist check failed for item {ItemResRef}.", item.ResRef);
        }

        matchedProduct = _shop.FindProduct(item.ResRef);
        if (matchedProduct is not null)
        {
            return true;
        }

    int baseItemTypeId = (int)item.BaseItem.ItemType;
    IReadOnlyList<NpcShopProduct> baseMatches = _shop.FindProductsByBaseItemType(baseItemTypeId);
        if (baseMatches.Count > 0)
        {
            matchedProduct = baseMatches.OrderBy(p => p.SortOrder).First();
            return true;
        }

        return false;
    }
}
