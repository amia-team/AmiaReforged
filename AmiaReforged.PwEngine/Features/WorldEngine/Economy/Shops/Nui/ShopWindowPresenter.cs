using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
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

    public ShopWindowPresenter(ShopWindowView view, NwPlayer player, NpcShop shop)
    {
        View = view;
        _player = player;
        _shop = shop;
    }

    [Inject] private Lazy<IShopItemBlacklist> ItemBlacklist { get; init; } = null!;
    [Inject] private Lazy<INpcShopItemFactory> ItemFactory { get; init; } = null!;

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
    List<bool> productPurchasable;
    List<string> productEntries = BuildProductEntries(out productPurchasable);
        List<string> inventoryEntries = BuildInventoryEntries();

        Token().SetBindValue(View.StoreTitle, BuildTitleText());
        Token().SetBindValue(View.StoreDescription, BuildDescriptionText());

        Token().SetBindValues(View.ProductEntries, productEntries);
    Token().SetBindValues(View.ProductPurchasable, productPurchasable);
        Token().SetBindValue(View.ProductCount, productEntries.Count);

        Token().SetBindValues(View.InventoryEntries, inventoryEntries);
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

    private List<string> BuildProductEntries(out List<bool> purchasable)
    {
        purchasable = new List<bool>();

        if (_shop.Products.Count == 0)
        {
            purchasable.Add(false);
            return new List<string> { "This merchant has nothing to sell." };
        }

        List<string> entries = new(_shop.Products.Count);

        foreach (NpcShopProduct product in _shop.Products)
        {
            string stockLabel = product.IsOutOfStock
                ? "Sold out"
                : string.Format(CultureInfo.InvariantCulture, "{0}/{1} on hand", product.CurrentStock,
                    product.MaxStock);

            string priceLabel = product.Price == 0
                ? "Complimentary"
                : string.Format(CultureInfo.InvariantCulture, "{0:N0} gp", product.Price);

            entries.Add($"{product.ResRef} — {priceLabel} ({stockLabel})");
            purchasable.Add(!product.IsOutOfStock);
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

        if (!product.TryConsume(1))
        {
            await NotifyAsync("Another customer just claimed the last one.", ColorConstants.Orange, refresh: true);
            return;
        }

        NwCreature? buyer = _player.ControlledCreature ?? _player.LoginCreature;
        if (buyer is null || !buyer.IsValid)
        {
            product.ReturnToStock(1);
            await NotifyAsync("You must be possessing your character to make a purchase.", ColorConstants.Orange);
            return;
        }

        try
        {
            NwItem? item = await ItemFactory.Value.CreateForInventoryAsync(buyer, product);
            if (item is null)
            {
                product.ReturnToStock(1);
                await NotifyAsync("The merchant cannot produce that item right now.", ColorConstants.Orange, refresh: true);
                return;
            }

            string itemName = string.IsNullOrWhiteSpace(item.Name) ? product.ResRef : item.Name;
            string message = BuildPurchaseMessage(itemName, product.Price);
            await NotifyAsync(message, ColorConstants.Lime, refresh: true);
        }
        catch (Exception ex)
        {
            product.ReturnToStock(1);
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

    private List<string> BuildInventoryEntries()
    {
        NwCreature? creature = _player.ControlledCreature ?? _player.LoginCreature;
        if (creature == null)
        {
            return new List<string> { "You are not possessing a creature." };
        }

        List<string> entries = new();

        foreach (NwItem item in creature.Inventory.Items)
        {
            if (!CanSell(item))
            {
                continue;
            }

            string itemName = string.IsNullOrWhiteSpace(item.Name) ? item.ResRef : item.Name;
            int stack = item.StackSize;
            string stackInfo = stack > 1 ? $" x{stack}" : string.Empty;

            entries.Add($"{itemName}{stackInfo}");
        }

        if (entries.Count == 0)
        {
            entries.Add("No items in your inventory may be sold here.");
        }

        return entries;
    }

    private bool CanSell(NwItem item)
    {
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

        if (_shop.FindProduct(item.ResRef) is null)
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

        return true;
    }
}
