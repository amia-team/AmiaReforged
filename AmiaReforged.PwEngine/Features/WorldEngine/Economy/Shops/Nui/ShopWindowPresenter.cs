using System;
using System.Collections.Generic;
using System.Globalization;
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
        List<string> productEntries = BuildProductEntries();
        List<string> inventoryEntries = BuildInventoryEntries();

        Token().SetBindValue(View.StoreTitle, BuildTitleText());
        Token().SetBindValue(View.StoreDescription, BuildDescriptionText());

        Token().SetBindValues(View.ProductEntries, productEntries);
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

    private List<string> BuildProductEntries()
    {
        if (_shop.Products.Count == 0)
        {
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
        }

        return entries;
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
