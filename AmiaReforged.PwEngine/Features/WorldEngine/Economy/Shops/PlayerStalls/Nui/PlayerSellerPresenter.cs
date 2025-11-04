using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class PlayerSellerPresenter : ScryPresenter<PlayerSellerView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PlayerSellerPresenter(PlayerSellerView view, NwPlayer player, PlayerStallSellerWindowConfig config)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override PlayerSellerView View { get; }

    private readonly NwPlayer _player;
    private readonly PlayerStallSellerWindowConfig _config;
    private readonly List<PlayerStallSellerProductView> _products = new();
    private readonly List<PlayerStallSellerInventoryItemView> _inventoryItems = new();

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private Guid? _sessionId;
    private long? _selectedProductId;
    private string? _selectedInventoryItemId;
    private bool _isProcessing;
    private bool _isClosing;
    private bool _rentFromCoinhouse;
    private bool _rentToggleVisible;
    private bool _rentToggleEnabled;
    private string _rentToggleTooltip = string.Empty;
    private PlayerStallSellerOperationResult? _lastOperationResult;

    [Inject] private PlayerStallEventManager EventManager { get; init; } = null!;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _config.Title)
        {
            Geometry = new NuiRect(90f, 70f, 920f, 520f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            NotifyError("The stall management window could not be configured.");
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            NotifyError("Unable to open the stall management window right now.");
            return;
        }

        _ = ApplySnapshotAsync(_config.InitialSnapshot);

        PlayerStallSellerEventCallbacks callbacks = new(
            snapshot => ApplySnapshotAsync(snapshot),
            result => HandleOperationResultAsync(result));

        try
        {
            _sessionId = EventManager.RegisterSellerSession(
                _config.StallId,
                _config.SellerPersona,
                callbacks);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register seller session for stall {StallId}.", _config.StallId);
            NotifyError("Failed to subscribe to stall updates.");
            return;
        }

        _ = RequestLatestSnapshotAsync();

    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click)
        {
            return;
        }

        if (eventData.ElementId == View.ManageButton.Id)
        {
            _ = HandleSelectProductAsync(eventData.ArrayIndex);
            return;
        }

        if (eventData.ElementId == View.UpdatePriceButton.Id)
        {
            _ = HandlePriceUpdateAsync();
            return;
        }

        if (eventData.ElementId == View.RentToggleButton.Id)
        {
            _ = HandleRentToggleAsync();
            return;
        }

        if (eventData.ElementId == View.RetrieveProductButton.Id)
        {
            _ = HandleRetrieveSelectedProductAsync();
            return;
        }

        if (eventData.ElementId == View.InventorySelectButton.Id)
        {
            _ = HandleSelectInventoryItemAsync(eventData.ArrayIndex);
            return;
        }

        if (eventData.ElementId == View.InventoryListButton.Id)
        {
            _ = HandleListSelectedInventoryItemAsync();
            return;
        }

        if (eventData.ElementId == View.CloseButton.Id)
        {
            RequestClose();
        }
    }

    public override void Close()
    {
        if (_isClosing)
        {
            return;
        }

    _isClosing = true;

        try
        {
            if (_sessionId is Guid sessionId)
            {
                EventManager.UnregisterSellerSession(sessionId);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to unregister seller session cleanly.");
        }

        try
        {
            _token.Close();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Player stall seller token close threw for player {PlayerName}.", _player.PlayerName);
        }
    }

    private void RequestClose()
    {
        if (_isClosing)
        {
            return;
        }

        RaiseCloseEvent();
        Close();
    }

    private async Task ApplySnapshotAsync(PlayerStallSellerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await NwTask.SwitchToMainThread();

        _products.Clear();
        _products.AddRange(snapshot.Products);

        Token().SetBindValue(View.StallTitle, snapshot.Summary.StallName);

        bool descriptionVisible = !string.IsNullOrWhiteSpace(snapshot.Summary.Description);
        Token().SetBindValue(View.StallDescriptionVisible, descriptionVisible);
        Token().SetBindValue(View.StallDescription, descriptionVisible ? snapshot.Summary.Description! : string.Empty);

        bool noticeVisible = !string.IsNullOrWhiteSpace(snapshot.Summary.Notice);
        Token().SetBindValue(View.StallNoticeVisible, noticeVisible);
        Token().SetBindValue(View.StallNotice, noticeVisible ? snapshot.Summary.Notice! : string.Empty);

        Token().SetBindValue(View.SellerName, FormatSellerName(snapshot.Seller));

    _rentFromCoinhouse = snapshot.RentFromCoinhouse;
    _rentToggleVisible = snapshot.RentToggleVisible;
    _rentToggleEnabled = snapshot.RentToggleEnabled;
    _rentToggleTooltip = snapshot.RentToggleTooltip ?? string.Empty;

        ApplyFeedback(snapshot.FeedbackVisible, snapshot.FeedbackMessage, snapshot.FeedbackColor ?? ColorConstants.White);
    ApplyRentToggleBindings();

        List<string> entries = new(snapshot.Products.Count);
        List<string> tooltips = new(snapshot.Products.Count);
        List<bool> managementEnabled = new(snapshot.Products.Count);

        foreach (PlayerStallSellerProductView product in snapshot.Products)
        {
            entries.Add(FormatProductEntry(product));
            tooltips.Add(string.IsNullOrWhiteSpace(product.Tooltip) ? string.Empty : product.Tooltip!);
            managementEnabled.Add(product.CanAdjustPrice);
        }

        Token().SetBindValues(View.ProductEntries, entries);
        Token().SetBindValues(View.ProductTooltips, tooltips);
        Token().SetBindValues(View.ProductManageEnabled, managementEnabled);
        Token().SetBindValue(View.ProductCount, entries.Count);

        _inventoryItems.Clear();
        if (snapshot.Inventory is not null)
        {
            _inventoryItems.AddRange(snapshot.Inventory);
        }

        List<string> inventoryEntries = new(_inventoryItems.Count);
        List<string> inventoryTooltips = new(_inventoryItems.Count);
        List<bool> inventoryEnabled = new(_inventoryItems.Count);

        foreach (PlayerStallSellerInventoryItemView item in _inventoryItems)
        {
            inventoryEntries.Add(FormatInventoryEntry(item));
            inventoryTooltips.Add(FormatInventoryTooltip(item));
            inventoryEnabled.Add(!_isProcessing);
        }

        Token().SetBindValues(View.InventoryEntries, inventoryEntries);
        Token().SetBindValues(View.InventoryTooltips, inventoryTooltips);
        Token().SetBindValues(View.InventorySelectEnabled, inventoryEnabled);
        Token().SetBindValue(View.InventoryCount, inventoryEntries.Count);
        Token().SetBindValue(View.InventoryEmptyVisible, inventoryEntries.Count == 0);

        string? targetInventoryId = _selectedInventoryItemId;
        if (targetInventoryId is not null && !_inventoryItems.Any(i => string.Equals(i.ObjectId, targetInventoryId, StringComparison.Ordinal)))
        {
            targetInventoryId = null;
        }

        await UpdateSelectedInventoryItemAsync(targetInventoryId).ConfigureAwait(false);

        long? targetProductId = snapshot.SelectedProductId;

        if (targetProductId is null && _selectedProductId is not null &&
            _products.Any(p => p.ProductId == _selectedProductId.Value))
        {
            targetProductId = _selectedProductId;
        }

        if (targetProductId is null && _products.Count > 0)
        {
            targetProductId = _products[0].ProductId;
        }

        await UpdateSelectedProductAsync(targetProductId).ConfigureAwait(false);
    }

    private async Task HandleSelectProductAsync(int rowIndex)
    {
        if (_isClosing)
        {
            return;
        }

        if (rowIndex < 0 || rowIndex >= _products.Count)
        {
            await UpdateSelectedProductAsync(null).ConfigureAwait(false);
            return;
        }

        PlayerStallSellerProductView product = _products[rowIndex];
        await UpdateSelectedProductAsync(product.ProductId).ConfigureAwait(false);
    }

    private async Task HandlePriceUpdateAsync()
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        PlayerStallSellerProductView? selected = TryGetSelectedProduct();
        if (selected is null)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Select an item to update its price.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        bool canAdjust = selected.CanAdjustPrice;
        if (!canAdjust)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "You are not allowed to adjust that price.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        string? rawPrice = Token().GetBindValue(View.PriceInput);
        if (!TryParsePrice(rawPrice, out int newPrice))
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Enter a valid non-negative price.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        if (newPrice == selected.Price)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "That item already uses that price.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallSellerPriceRequest request = new(
                sessionId,
                _config.StallId,
                selected.ProductId,
                _config.SellerPersona,
                newPrice);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestUpdatePriceAsync(request)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                await HandleOperationResultAsync(result).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while processing stall price update for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't update that price.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private async Task HandleOperationResultAsync(PlayerStallSellerOperationResult result)
    {
        if (ReferenceEquals(result, _lastOperationResult))
        {
            return;
        }

        _lastOperationResult = result;

        Color color = result.MessageColor ?? (result.Success ? ColorConstants.Lime : ColorConstants.Orange);

        await NwTask.SwitchToMainThread();
        ApplyFeedback(!string.IsNullOrWhiteSpace(result.Message), result.Message, color);

        if (result.Snapshot is not null)
        {
            await ApplySnapshotAsync(result.Snapshot).ConfigureAwait(false);
        }
    }

    private async Task UpdateSelectedProductAsync(long? productId)
    {
        await NwTask.SwitchToMainThread();

        PlayerStallSellerProductView? product = productId is null
            ? null
            : _products.FirstOrDefault(p => p.ProductId == productId.Value);

        _selectedProductId = product?.ProductId;

        bool visible = product is not null;
        Token().SetBindValue(View.DetailVisible, visible);
        Token().SetBindValue(View.DetailPlaceholderVisible, !visible);

        if (product is null)
        {
            Token().SetBindValue(View.SelectedProductName, string.Empty);
            Token().SetBindValue(View.SelectedProductQuantity, string.Empty);
            Token().SetBindValue(View.SelectedProductStatus, string.Empty);
            Token().SetBindValue(View.SelectedProductPrice, string.Empty);
            Token().SetBindValue(View.SelectedProductDescriptionVisible, false);
            Token().SetBindValue(View.SelectedProductDescription, string.Empty);
            Token().SetBindValue(View.PriceInput, string.Empty);
            Token().SetBindValue(View.PriceInputEnabled, false);
            Token().SetBindValue(View.PriceSaveEnabled, false);
            Token().SetBindValue(View.ProductRetrieveEnabled, false);
            return;
        }

        Token().SetBindValue(View.SelectedProductName, product.DisplayName);
        Token().SetBindValue(View.SelectedProductQuantity, FormatQuantity(product));
        Token().SetBindValue(View.SelectedProductStatus, FormatStatus(product));
        Token().SetBindValue(View.SelectedProductPrice, FormatCurrentPrice(product.Price));

        bool descriptionVisible = !string.IsNullOrWhiteSpace(product.Tooltip);
        Token().SetBindValue(View.SelectedProductDescriptionVisible, descriptionVisible);
        Token().SetBindValue(View.SelectedProductDescription, descriptionVisible ? product.Tooltip! : string.Empty);

        Token().SetBindValue(View.PriceInput, product.Price.ToString(CultureInfo.InvariantCulture));

        bool allowEditing = product.CanAdjustPrice && !_isProcessing;
        Token().SetBindValue(View.PriceInputEnabled, allowEditing);
        Token().SetBindValue(View.PriceSaveEnabled, allowEditing);
    bool allowRetrieve = allowEditing && !product.IsSoldOut;
    Token().SetBindValue(View.ProductRetrieveEnabled, allowRetrieve);
    }

    private async Task HandleRentToggleAsync()
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        if (!_rentToggleEnabled)
        {
            if (!string.IsNullOrWhiteSpace(_rentToggleTooltip))
            {
                await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                    _rentToggleTooltip,
                    ColorConstants.Orange)).ConfigureAwait(false);
            }

            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            bool targetState = !_rentFromCoinhouse;

            PlayerStallRentSourceRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                targetState);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestUpdateRentSourceAsync(request)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                await HandleOperationResultAsync(result).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while toggling stall rent source for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't update the rent payment source.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private PlayerStallSellerProductView? TryGetSelectedProduct()
    {
        if (_selectedProductId is null)
        {
            return null;
        }

        return _products.FirstOrDefault(p => p.ProductId == _selectedProductId.Value);
    }

    private PlayerStallSellerInventoryItemView? TryGetSelectedInventoryItem()
    {
        if (_selectedInventoryItemId is null)
        {
            return null;
        }

        return _inventoryItems.FirstOrDefault(item => string.Equals(item.ObjectId, _selectedInventoryItemId, StringComparison.Ordinal));
    }

    private async Task SetProcessingStateAsync(bool processing)
    {
        if (_isProcessing == processing)
        {
            return;
        }

        _isProcessing = processing;
        await UpdateSelectedProductAsync(_selectedProductId).ConfigureAwait(false);
        await UpdateSelectedInventoryItemAsync(_selectedInventoryItemId).ConfigureAwait(false);
        await NwTask.SwitchToMainThread();
        ApplyRentToggleBindings();
        ApplyInventoryListBindings();
    }

    private async Task UpdateSelectedInventoryItemAsync(string? objectId)
    {
        await NwTask.SwitchToMainThread();

        _selectedInventoryItemId = objectId;

        PlayerStallSellerInventoryItemView? item = objectId is null
            ? null
            : _inventoryItems.FirstOrDefault(i => string.Equals(i.ObjectId, objectId, StringComparison.Ordinal));

        bool visible = item is not null;
        Token().SetBindValue(View.InventoryDetailVisible, visible);
        Token().SetBindValue(View.InventoryDetailPlaceholderVisible, !visible);

        if (item is null)
        {
            Token().SetBindValue(View.InventorySelectedName, string.Empty);
            Token().SetBindValue(View.InventorySelectedResRef, string.Empty);
            Token().SetBindValue(View.InventorySelectedQuantity, string.Empty);
            Token().SetBindValue(View.InventoryPriceInput, string.Empty);
        }
        else
        {
            Token().SetBindValue(View.InventorySelectedName, item.DisplayName);
            Token().SetBindValue(View.InventorySelectedResRef, string.Format(CultureInfo.InvariantCulture, "ResRef: {0}", item.ResRef));
            Token().SetBindValue(View.InventorySelectedQuantity, item.IsStackable
                ? string.Format(CultureInfo.InvariantCulture, "Stack size: {0:n0}", Math.Max(1, item.Quantity))
                : "Stack size: 1");
            Token().SetBindValue(View.InventoryPriceInput, string.Empty);
        }

        ApplyInventorySelectionBindings();
    }

    private void ApplyInventoryListBindings()
    {
        if (_inventoryItems.Count == 0)
        {
            Token().SetBindValues(View.InventorySelectEnabled, Array.Empty<bool>());
            return;
        }

        bool rowEnabled = !_isProcessing;
        List<bool> enabled = new(_inventoryItems.Count);
        for (int i = 0; i < _inventoryItems.Count; i++)
        {
            enabled.Add(rowEnabled);
        }

        Token().SetBindValues(View.InventorySelectEnabled, enabled);
    }

    private void ApplyInventorySelectionBindings()
    {
        bool hasSelection = _selectedInventoryItemId is not null;
        bool allowActions = hasSelection && !_isProcessing;

        Token().SetBindValue(View.InventoryPriceEnabled, allowActions);
        Token().SetBindValue(View.InventoryListEnabled, allowActions);
    }

    private async Task HandleSelectInventoryItemAsync(int rowIndex)
    {
        if (_isClosing)
        {
            return;
        }

        if (rowIndex < 0 || rowIndex >= _inventoryItems.Count)
        {
            await UpdateSelectedInventoryItemAsync(null).ConfigureAwait(false);
            return;
        }

        PlayerStallSellerInventoryItemView item = _inventoryItems[rowIndex];
        await UpdateSelectedInventoryItemAsync(item.ObjectId).ConfigureAwait(false);
    }

    private async Task HandleListSelectedInventoryItemAsync()
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        PlayerStallSellerInventoryItemView? item = TryGetSelectedInventoryItem();
        if (item is null)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Select an inventory item to list.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        string? rawPrice = Token().GetBindValue(View.InventoryPriceInput);
        if (!TryParsePrice(rawPrice, out int price) || price <= 0)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Enter a price greater than zero to list that item.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallSellerListItemRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                item.ObjectId,
                price);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestListInventoryItemAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while listing inventory item for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't list that item right now.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private async Task HandleRetrieveSelectedProductAsync()
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        PlayerStallSellerProductView? selected = TryGetSelectedProduct();
        if (selected is null)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Select a listing to take it back.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        if (selected.IsSoldOut)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "That listing has already sold out.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallSellerRetrieveProductRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                selected.ProductId);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestRetrieveProductAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reclaiming stall inventory for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't return that listing right now.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private static string FormatInventoryEntry(PlayerStallSellerInventoryItemView item)
    {
        string quantity = item.IsStackable
            ? string.Format(CultureInfo.InvariantCulture, "(x{0:n0})", Math.Max(1, item.Quantity))
            : "(x1)";

        return string.Format(CultureInfo.InvariantCulture, "{0} {1}", item.DisplayName, quantity);
    }

    private static string FormatInventoryTooltip(PlayerStallSellerInventoryItemView item)
    {
        List<string> lines = new()
        {
            string.Format(CultureInfo.InvariantCulture, "ResRef: {0}", item.ResRef)
        };

        if (item.BaseItemType.HasValue)
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, "Base item type: {0}", item.BaseItemType.Value));
        }

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            lines.Add(string.Empty);
            lines.Add(item.Description!);
        }

        return string.Join('\n', lines);
    }

    private async Task RequestLatestSnapshotAsync()
    {
        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        try
        {
            PlayerStallSellerOperationResult result = await EventManager
                .RequestSellerSnapshotAsync(sessionId)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                await HandleOperationResultAsync(result).ConfigureAwait(false);
                return;
            }

            if (result.Snapshot is not null)
            {
                await ApplySnapshotAsync(result.Snapshot).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to request seller snapshot for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't refresh the stall state.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
    }

    private void ApplyFeedback(bool visible, string? message, Color color)
    {
        Token().SetBindValue(View.FeedbackVisible, visible);
        Token().SetBindValue(View.FeedbackText, visible && !string.IsNullOrWhiteSpace(message) ? message! : string.Empty);
        Token().SetBindValue(View.FeedbackColor, color);
    }

    private void ApplyRentToggleBindings()
    {
        Token().SetBindValue(View.RentToggleVisible, _rentToggleVisible);
        Token().SetBindValue(View.RentToggleEnabled, _rentToggleEnabled && !_isProcessing);
        Token().SetBindValue(View.RentToggleLabel, FormatRentToggleLabel());
        Token().SetBindValue(View.RentToggleStatus, FormatRentToggleStatus());
        Token().SetBindValue(View.RentToggleTooltip, string.IsNullOrWhiteSpace(_rentToggleTooltip) ? string.Empty : _rentToggleTooltip);
    }

    private void NotifyError(string message)
    {
        if (_player.IsValid)
        {
            _player.SendServerMessage(message, ColorConstants.Red);
        }
    }

    private static string FormatSellerName(PlayerStallSellerContext context)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "Managing as {0}",
            string.IsNullOrWhiteSpace(context.SellerDisplayName)
                ? context.SellerPersona.ToString()
                : context.SellerDisplayName);
    }

    private static string FormatProductEntry(PlayerStallSellerProductView product)
    {
        string price = FormatPrice(product.Price);
        string status = product.IsActive
            ? product.IsSoldOut
                ? "(Sold out)"
                : string.Format(CultureInfo.InvariantCulture, "(Qty: {0:n0})", Math.Max(0, product.QuantityAvailable))
            : "(Inactive)";

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} - {1} {2}",
            product.DisplayName,
            price,
            status);
    }

    private static string FormatQuantity(PlayerStallSellerProductView product)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "On hand: {0:n0}",
            Math.Max(0, product.QuantityAvailable));
    }

    private static string FormatStatus(PlayerStallSellerProductView product)
    {
        if (!product.IsActive)
        {
            return "Status: Inactive";
        }

        if (product.IsSoldOut)
        {
            return "Status: Sold out";
        }

        return "Status: Active";
    }

    private static string FormatCurrentPrice(int price)
    {
        return string.Format(CultureInfo.InvariantCulture, "Current price: {0}", FormatPrice(price));
    }

    private string FormatRentToggleLabel()
    {
        return _rentFromCoinhouse
            ? "Use Stall Earnings"
            : "Use Coinhouse Account";
    }

    private string FormatRentToggleStatus()
    {
        return _rentFromCoinhouse
            ? "Rent payments: Coinhouse account"
            : "Rent payments: Stall earnings";
    }

    private static string FormatPrice(int price)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:n0} gp", Math.Max(0, price));
    }

    private static bool TryParsePrice(string? text, out int value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return false;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            value = 0;
            return false;
        }

        if (parsed < 0)
        {
            value = 0;
            return false;
        }

        value = parsed;
        return true;
    }
}
