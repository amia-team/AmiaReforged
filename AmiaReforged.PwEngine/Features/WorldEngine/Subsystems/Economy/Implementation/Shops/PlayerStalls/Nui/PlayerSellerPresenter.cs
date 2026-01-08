using System.Globalization;
using System.Text;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Time;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Nui;

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
    private readonly List<PlayerStallLedgerEntryView> _ledgerEntries = new();

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
    private bool _holdEarningsInStall;
    private bool _holdToggleVisible;
    private bool _holdToggleEnabled;
    private string _holdToggleTooltip = string.Empty;
    private string _holdToggleLabel = "Hold profits in stall escrow";
    private int _escrowBalance;
    private int _currentPeriodGrossProfits;
    private string _grossProfitsText = string.Empty;
    private string _availableFundsText = string.Empty;
    private bool _earningsVisible;
    private bool _withdrawEnabled;
    private bool _withdrawAllEnabled;
    private string _earningsTooltip = string.Empty;
    private string _depositInput = string.Empty;
    private bool _depositEnabled;
    private string _depositTooltip = string.Empty;
    private PlayerStallSellerOperationResult? _lastOperationResult;
    private readonly List<PlayerStallMemberView> _members = new();
    private bool _isOwner;
    private bool _canManageMembers;

    [Inject] private PlayerStallEventManager EventManager { get; init; } = null!;
    [Inject] private IHarptosTimeService HarptosTimeService { get; init; } = null!;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _config.Title)
        {
            Geometry = new NuiRect(90f, 70f, 850f, 770f),
            Resizable = true
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
            ApplySnapshotAsync,
            HandleOperationResultAsync);

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
        if (eventData.ElementId == PlayerSellerView.HoldEarningsToggleId && eventData.EventType == NuiEventType.MouseDown)
        {
            Log.Info($"Woop {eventData.EventType}");
            _ = HandleHoldEarningsToggleAsync();
            return;
        }

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

        if (View.RentToggleButton != null && eventData.ElementId == View.RentToggleButton.Id)
        {
            _ = HandleRentToggleAsync();
            return;
        }

        if (eventData.ElementId == View.WithdrawProfitsButton.Id)
        {
            _ = HandleWithdrawProfitsAsync(false);
            return;
        }

        if (eventData.ElementId == View.WithdrawAllProfitsButton.Id)
        {
            _ = HandleWithdrawProfitsAsync(true);
            return;
        }

        if (eventData.ElementId == View.DepositButton.Id)
        {
            _ = HandleDepositAsync();
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

        if (eventData.ElementId == View.ViewDescriptionButton.Id)
        {
            HandleViewFullDescription();
            return;
        }

        if (View.RemoveMemberButton != null && eventData.ElementId == View.RemoveMemberButton.Id)
        {
            _ = HandleRemoveMemberAsync(eventData.ArrayIndex);
            return;
        }

        if (View.AddMemberButton != null && eventData.ElementId == View.AddMemberButton.Id)
        {
            _ = HandleAddMemberAsync();
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
        _holdEarningsInStall = snapshot.HoldEarningsInStall;
        _holdToggleVisible = snapshot.HoldEarningsToggleVisible;
        _holdToggleEnabled = snapshot.HoldEarningsToggleEnabled;
        _holdToggleTooltip = snapshot.HoldEarningsToggleTooltip ?? string.Empty;
        _holdToggleLabel = string.IsNullOrWhiteSpace(snapshot.HoldEarningsToggleLabel)
            ? "Hold profits in stall escrow"
            : snapshot.HoldEarningsToggleLabel!;
        _escrowBalance = Math.Max(0, snapshot.EscrowBalance);
        _currentPeriodGrossProfits = Math.Max(0, snapshot.CurrentPeriodGrossProfits);
        _grossProfitsText = FormatPrice(_currentPeriodGrossProfits);
        _availableFundsText = FormatPrice(_escrowBalance);
        _earningsVisible = snapshot.EarningsRowVisible;
        _withdrawEnabled = snapshot.WithdrawEnabled;
        _withdrawAllEnabled = snapshot.WithdrawAllEnabled;
        _earningsTooltip = snapshot.EarningsTooltip ?? string.Empty;
        _depositEnabled = snapshot.DepositEnabled;
        _depositTooltip = snapshot.DepositTooltip ?? string.Empty;

        ApplyFeedback(snapshot.FeedbackVisible, snapshot.FeedbackMessage,
            snapshot.FeedbackColor ?? ColorConstants.White);
        ApplyRentToggleBindings();
        ApplyHoldEarningsBindings();
        ApplyEarningsBindings();
        Token().SetBindValue(View.EarningsWithdrawInput, string.Empty);
        Token().SetBindValue(View.DepositInput, string.Empty);

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
        Token().SetBindValue(View.ProductEmptyVisible, entries.Count == 0);

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

        _ledgerEntries.Clear();
        if (snapshot.LedgerEntries is not null)
        {
            _ledgerEntries.AddRange(snapshot.LedgerEntries);
        }

        List<string> ledgerTimestamps = new(_ledgerEntries.Count);
        List<string> ledgerAmounts = new(_ledgerEntries.Count);
        List<string> ledgerDescriptions = new(_ledgerEntries.Count);
        List<string> ledgerTooltips = new(_ledgerEntries.Count);

        foreach (PlayerStallLedgerEntryView entry in _ledgerEntries)
        {
            string timestampDisplay = FormatLedgerTimestamp(entry);
            string amountDisplay = FormatLedgerAmount(entry.Amount, entry.Currency);
            string descriptionDisplay = entry.Description ?? string.Empty;

            ledgerTimestamps.Add(timestampDisplay);
            ledgerAmounts.Add(amountDisplay);
            ledgerDescriptions.Add(descriptionDisplay);
            ledgerTooltips.Add(BuildLedgerTooltip(timestampDisplay, amountDisplay, descriptionDisplay));
        }

        Token().SetBindValues(View.LedgerTimestampEntries, ledgerTimestamps);
        Token().SetBindValues(View.LedgerAmountEntries, ledgerAmounts);
        Token().SetBindValues(View.LedgerDescriptionEntries, ledgerDescriptions);
        Token().SetBindValues(View.LedgerTooltipEntries, ledgerTooltips);
        Token().SetBindValue(View.LedgerCount, ledgerTimestamps.Count);

        // Apply member data
        _isOwner = snapshot.IsOwner;
        _canManageMembers = snapshot.CanManageMembers;
        _members.Clear();
        if (snapshot.Members is not null)
        {
            _members.AddRange(snapshot.Members);
        }

        ApplyMemberBindings();

        string? targetInventoryId = _selectedInventoryItemId;
        if (targetInventoryId is not null &&
            !_inventoryItems.Any(i => string.Equals(i.ObjectId, targetInventoryId, StringComparison.Ordinal)))
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

    private async Task HandleHoldEarningsToggleAsync()
    {
        Log.Info("Checkbox clicked");
        if (_isClosing || _isProcessing)
        {
            Log.Info($"Is closing: {_isClosing}, is processing: {_isProcessing}");
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            Log.Info($"Invalid session ID: {_sessionId}");
            return;
        }

        if (!_holdToggleEnabled)
        {
            if (!string.IsNullOrWhiteSpace(_holdToggleTooltip))
            {
                await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                    _holdToggleTooltip,
                    ColorConstants.Orange)).ConfigureAwait(false);
            }

            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.HoldEarningsChecked, _holdEarningsInStall);
            return;
        }
        bool desired = !_holdEarningsInStall;

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallHoldEarningsRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                desired);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestUpdateHoldEarningsAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);

            if (!result.Success)
            {
                await NwTask.SwitchToMainThread();
                Token().SetBindValue(View.HoldEarningsChecked, _holdEarningsInStall);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while updating hold earnings for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't update how stall profits are handled.",
                ColorConstants.Red)).ConfigureAwait(false);

            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.HoldEarningsChecked, _holdEarningsInStall);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private async Task HandleWithdrawProfitsAsync(bool withdrawAll)
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        int? requestedAmount = null;

        if (!withdrawAll)
        {
            string? rawAmount = Token().GetBindValue(View.EarningsWithdrawInput);
            if (!TryParseWithdrawalAmount(rawAmount, out int parsedAmount))
            {
                await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                    "Enter a valid withdrawal amount greater than zero.",
                    ColorConstants.Orange)).ConfigureAwait(false);
                return;
            }

            requestedAmount = parsedAmount;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallWithdrawRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                requestedAmount);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestWithdrawEarningsAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);

            if (result.Success)
            {
                await NwTask.SwitchToMainThread();
                Token().SetBindValue(View.EarningsWithdrawInput, string.Empty);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while withdrawing stall earnings for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't withdraw stall earnings.",
                ColorConstants.Red)). ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private async Task HandleDepositAsync()
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        if (!_depositEnabled)
        {
            if (!string.IsNullOrWhiteSpace(_depositTooltip))
            {
                await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                    _depositTooltip,
                    ColorConstants.Orange)).ConfigureAwait(false);
            }

            return;
        }

        string? rawAmount = Token().GetBindValue(View.DepositInput);
        if (!TryParseWithdrawalAmount(rawAmount, out int depositAmount))
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Enter a valid deposit amount greater than zero.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallDepositRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                _player.ControlledCreature?.Name ?? _player.LoginCreature?.Name ?? "Unknown",
                depositAmount);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestDepositRentAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);

            if (result.Success)
            {
                await NwTask.SwitchToMainThread();
                Token().SetBindValue(View.DepositInput, string.Empty);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while depositing stall earnings for stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "We couldn't deposit stall earnings.",
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

        return _inventoryItems.FirstOrDefault(item =>
            string.Equals(item.ObjectId, _selectedInventoryItemId, StringComparison.Ordinal));
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
        ApplyHoldEarningsBindings();
        ApplyEarningsBindings();
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
            Token().SetBindValue(View.InventorySelectedResRef,
                string.Format(CultureInfo.InvariantCulture, "ResRef: {0}", item.ResRef));
            Token().SetBindValue(View.InventorySelectedQuantity, item.IsStackable
                ? string.Format(CultureInfo.InvariantCulture, "Stack size: {0:n0}", Math.Max(1, item.Quantity))
                : "Stack size: 1");
            Token().SetBindValue(View.InventoryPriceInput, string.Empty);
        }

        ApplyInventorySelectionBindings();
    }

    private string FormatLedgerTimestamp(PlayerStallLedgerEntryView entry)
    {
        HarptosDateTime harptos = ConvertToHarptos(entry.OccurredUtc);
        return harptos.ToDisplayString(useDiegeticTime: true, includeTime: false);
    }

    private HarptosDateTime ConvertToHarptos(DateTime occurredUtc)
    {
        DateTimeOffset localTime = NormalizeToLocal(occurredUtc);
        return HarptosTimeService.Convert(localTime);
    }

    private static DateTimeOffset NormalizeToLocal(DateTime occurredUtc)
    {
        DateTime universal = occurredUtc.Kind switch
        {
            DateTimeKind.Utc => occurredUtc,
            DateTimeKind.Local => occurredUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc)
        };

        return new DateTimeOffset(universal, TimeSpan.Zero).ToLocalTime();
    }

    private static string FormatLedgerAmount(int amount, string currency)
    {
        string unit = string.IsNullOrWhiteSpace(currency) ? "gp" : currency;
    return string.Format(CultureInfo.InvariantCulture, "{0:+#,##0;-#,##0;0} {1}", amount, unit);
    }

    private static string BuildLedgerTooltip(string timestampText, string amountText, string descriptionText)
    {
        List<string> parts = new(3);

        if (!string.IsNullOrWhiteSpace(timestampText))
        {
            parts.Add(timestampText);
        }

        if (!string.IsNullOrWhiteSpace(amountText))
        {
            parts.Add(amountText);
        }

        if (!string.IsNullOrWhiteSpace(descriptionText))
        {
            parts.Add(descriptionText);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
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

    private void HandleViewFullDescription()
    {
        if (_isClosing)
        {
            return;
        }

        PlayerStallSellerProductView? product = TryGetSelectedProduct();
        if (product is null || string.IsNullOrWhiteSpace(product.Tooltip))
        {
            return;
        }

        ProductDescriptionPresenter descriptionPresenter = new(_player, product.Tooltip, product.ItemTypeName);
        descriptionPresenter.Create();
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
        Token().SetBindValue(View.FeedbackText,
            visible && !string.IsNullOrWhiteSpace(message) ? message! : string.Empty);
        Token().SetBindValue(View.FeedbackColor, color);
    }

    private void ApplyRentToggleBindings()
    {
        Token().SetBindValue(View.RentToggleVisible, _rentToggleVisible);
        Token().SetBindValue(View.RentToggleEnabled, _rentToggleEnabled && !_isProcessing);
        Token().SetBindValue(View.RentToggleLabel, FormatRentToggleLabel());
        Token().SetBindValue(View.RentToggleStatus, FormatRentToggleStatus());
        Token().SetBindValue(View.RentToggleTooltip,
            string.IsNullOrWhiteSpace(_rentToggleTooltip) ? string.Empty : _rentToggleTooltip);
    }

    private void ApplyHoldEarningsBindings()
    {
        Token().SetBindValue(View.HoldEarningsVisible, _holdToggleVisible);
        Token().SetBindValue(View.HoldEarningsEnabled, _holdToggleEnabled && !_isProcessing);
        Token().SetBindValue(View.HoldEarningsChecked, _holdEarningsInStall);
        Token().SetBindValue(View.HoldEarningsTooltip,
            string.IsNullOrWhiteSpace(_holdToggleTooltip) ? string.Empty : _holdToggleTooltip);
        Token().SetBindValue(View.HoldEarningsLabel, BuildHoldEarningsPlaceholderText());
    }

    private void ApplyEarningsBindings()
    {
        Token().SetBindValue(View.EarningsRowVisible, _earningsVisible);
        Token().SetBindValue(View.GrossProfitsText, _grossProfitsText);
        Token().SetBindValue(View.AvailableFundsText, _availableFundsText);
        Token().SetBindValue(View.EarningsBalanceText, FormatEarningsBalance(_escrowBalance, _currentPeriodGrossProfits));
        Token().SetBindValue(View.EarningsTooltip,
            string.IsNullOrWhiteSpace(_earningsTooltip) ? string.Empty : _earningsTooltip);
        Token().SetBindValue(View.EarningsWithdrawEnabled, _withdrawEnabled && !_isProcessing);
        Token().SetBindValue(View.EarningsWithdrawAllEnabled, _withdrawAllEnabled && !_isProcessing);
        Token().SetBindValue(View.EarningsInputEnabled, _withdrawEnabled && !_isProcessing);
        Token().SetBindValue(View.DepositEnabled, _depositEnabled && !_isProcessing);
        Token().SetBindValue(View.DepositTooltip,
            string.IsNullOrWhiteSpace(_depositTooltip) ? string.Empty : _depositTooltip);
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

    private string BuildHoldEarningsPlaceholderText()
    {
        string label = string.IsNullOrWhiteSpace(_holdToggleLabel)
            ? "Hold profits in stall escrow"
            : _holdToggleLabel;
        string state = _holdEarningsInStall ? "on" : "off";

        return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", label, state);
    }

    private static string FormatEarningsBalance(int escrowBalance, int grossProfits)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Gross: {0:n0} gp | Available: {1:n0} gp",
            Math.Max(0, grossProfits),
            Math.Max(0, escrowBalance));
    }

    private static string FormatPrice(int price)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:n0} gp", Math.Max(0, price));
    }

    private static bool TryParseWithdrawalAmount(string? text, out int value)
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

        if (parsed <= 0)
        {
            value = 0;
            return false;
        }

        value = parsed;
        return true;
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

    private void ApplyMemberBindings()
    {
        bool sectionVisible = _isOwner || _members.Count > 0;
        Token().SetBindValue(View.MemberSectionVisible, sectionVisible);
        Token().SetBindValue(View.AddMemberVisible, _canManageMembers);
        Token().SetBindValue(View.AddMemberEnabled, _canManageMembers && !_isProcessing);
        Token().SetBindValue(View.AddMemberInput, string.Empty);

        List<string> memberNames = new(_members.Count);
        List<string> memberTooltips = new(_members.Count);
        List<bool> removeEnabled = new(_members.Count);

        foreach (PlayerStallMemberView member in _members)
        {
            string displayName = member.IsOwner 
                ? $"{member.DisplayName} (Owner)" 
                : member.DisplayName;
            memberNames.Add(displayName);
            memberTooltips.Add(BuildMemberTooltip(member));
            removeEnabled.Add(member.CanRemove && !_isProcessing);
        }

        Token().SetBindValues(View.MemberNames, memberNames);
        Token().SetBindValues(View.MemberTooltips, memberTooltips);
        Token().SetBindValues(View.MemberRemoveEnabled, removeEnabled);
        Token().SetBindValue(View.MemberCount, memberNames.Count);
    }

    private static string BuildMemberTooltip(PlayerStallMemberView member)
    {
        StringBuilder sb = new();
        sb.AppendLine(member.DisplayName);
        if (member.IsOwner)
        {
            sb.AppendLine("Role: Owner");
        }
        else
        {
            sb.AppendLine("Role: Member");
        }
        sb.AppendLine();
        sb.Append("Permissions: ");
        List<string> perms = new();
        if (member.CanManageInventory) perms.Add("Inventory");
        if (member.CanConfigureSettings) perms.Add("Settings");
        if (member.CanCollectEarnings) perms.Add("Earnings");
        sb.Append(perms.Count > 0 ? string.Join(", ", perms) : "None");
        return sb.ToString();
    }

    private async Task HandleAddMemberAsync()
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        if (!_canManageMembers)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Only the stall owner can add members.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        string? memberName = Token().GetBindValue(View.AddMemberInput);
        if (string.IsNullOrWhiteSpace(memberName))
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Enter a character name to add as a member.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            // Try to find the player by character name and get their persona
            PersonaId? memberPersona = TryResolvePersonaByCharacterName(memberName.Trim());
            if (memberPersona is null)
            {
                await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                    $"Could not find character '{memberName}'. They must be online.",
                    ColorConstants.Orange)).ConfigureAwait(false);
                return;
            }

            PlayerStallAddMemberRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                memberPersona.Value,
                memberName.Trim());

            PlayerStallSellerOperationResult result = await EventManager
                .RequestAddMemberAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while adding member to stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Failed to add member.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private async Task HandleRemoveMemberAsync(int rowIndex)
    {
        if (_isClosing || _isProcessing)
        {
            return;
        }

        if (_sessionId is not Guid sessionId)
        {
            return;
        }

        if (rowIndex < 0 || rowIndex >= _members.Count)
        {
            return;
        }

        PlayerStallMemberView member = _members[rowIndex];
        if (!member.CanRemove)
        {
            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "You cannot remove that member.",
                ColorConstants.Orange)).ConfigureAwait(false);
            return;
        }

        await SetProcessingStateAsync(true).ConfigureAwait(false);

        try
        {
            PlayerStallRemoveMemberRequest request = new(
                sessionId,
                _config.StallId,
                _config.SellerPersona,
                member.PersonaId);

            PlayerStallSellerOperationResult result = await EventManager
                .RequestRemoveMemberAsync(request)
                .ConfigureAwait(false);

            await HandleOperationResultAsync(result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while removing member from stall {StallId}.", _config.StallId);

            await HandleOperationResultAsync(PlayerStallSellerOperationResult.Fail(
                "Failed to remove member.",
                ColorConstants.Red)).ConfigureAwait(false);
        }
        finally
        {
            await SetProcessingStateAsync(false).ConfigureAwait(false);
        }
    }

    private PersonaId? TryResolvePersonaByCharacterName(string characterName)
    {
        // Try to find online player by character name
        NwPlayer? targetPlayer = NwModule.Instance.Players
            .FirstOrDefault(p => 
                p.ControlledCreature is not null && 
                string.Equals(p.ControlledCreature.Name, characterName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer?.ControlledCreature is null)
        {
            return null;
        }

        // Get the character's persona ID from their GUID
        Guid characterGuid = targetPlayer.ControlledCreature.GetObjectVariable<LocalVariableGuid>("CHARACTER_GUID").Value;
        if (characterGuid == Guid.Empty)
        {
            return null;
        }

        return PersonaId.FromCharacter(new CharacterId(characterGuid));
    }
}
