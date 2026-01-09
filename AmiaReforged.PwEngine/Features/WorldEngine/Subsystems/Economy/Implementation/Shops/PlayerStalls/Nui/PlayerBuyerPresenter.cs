using System.Globalization;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Nui;

public sealed class PlayerBuyerPresenter : ScryPresenter<PlayerBuyerView>, IAutoCloseOnMove
{
	private static readonly Logger Log = LogManager.GetCurrentClassLogger();

	private readonly NwPlayer _player;
	private readonly PlayerStallBuyerWindowConfig _config;
	private readonly List<ProductRow> _productRows = new();

	private NuiWindowToken _token;
	private NuiWindow? _window;
	private Guid? _sessionId;
	private bool _isProcessing;
	private bool _isClosing;
	private int _selectedProductIndex = -1;
	private int _selectedProductQuantityAvailable;
	private int _selectedProductUnitPrice;
	private int _currentGoldOnHand;

	// Filter state
	private string _searchTerm = string.Empty;
	private int _selectedBaseItemType = -1; // -1 = All Types
	private PlayerStallBuyerSnapshot? _currentSnapshot;

	public PlayerBuyerPresenter(PlayerBuyerView view, NwPlayer player, PlayerStallBuyerWindowConfig config)
	{
		View = view;
		_player = player;
		_config = config;
	}

	public override PlayerBuyerView View { get; }

	[Inject] private PlayerStallEventManager EventManager { get; init; } = null!;

	public override NuiWindowToken Token() => _token;

	public override void InitBefore()
	{
		_window = new NuiWindow(View.RootLayout(), _config.Title)
		{
			Geometry = new NuiRect(80f, 80f, 950f, 520f),
			Resizable = true
		};
	}

	public override void Create()
	{
		if (_window is null)
		{
			NotifyError("The stall window could not be configured.");
			return;
		}

		if (!_player.TryCreateNuiWindow(_window, out _token))
		{
			NotifyError("Unable to open the stall window right now.");
			return;
		}

		_ = ApplySnapshotAsync(_config.InitialSnapshot);

		PlayerStallBuyerEventCallbacks callbacks = new(
			snapshot => ApplySnapshotAsync(snapshot),
			result => HandlePurchaseResultAsync(result));

		try
		{
			_sessionId = EventManager.RegisterBuyerSession(_config.StallId, _config.BuyerPersona, callbacks);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to register buyer session for stall {StallId}.", _config.StallId);
			NotifyError("Failed to subscribe to stall updates.");
		}
	}

	public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
	{
		if (eventData.EventType == NuiEventType.Watch)
		{
			HandleWatchEvent(eventData);
			return;
		}

		if (eventData.EventType != NuiEventType.Click)
		{
			return;
		}

		if (eventData.ElementId == "player_stall_clear_search")
		{
			_searchTerm = string.Empty;
			Token().SetBindValue(View.SearchFilter, string.Empty);
			RefreshProductList();
			return;
		}

		if (eventData.ElementId == View.SelectButton.Id)
		{
			int rowIndex = eventData.ArrayIndex;
			HandleSelectProduct(rowIndex);
			return;
		}

		if (eventData.ElementId == View.BuyFromPreviewButton.Id)
		{
			_ = HandlePurchaseAsync(_selectedProductIndex);
			return;
		}

		if (eventData.ElementId == View.LeaveButton.Id)
		{
			RaiseCloseEvent();
			Close();
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
				EventManager.UnregisterBuyerSession(sessionId);
			}
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Failed to unregister buyer session cleanly.");
		}

		try
		{
			_token.Close();
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Player stall buyer token close threw for player {PlayerName}.", _player.PlayerName);
		}
	}

	private async Task ApplySnapshotAsync(PlayerStallBuyerSnapshot snapshot)
	{
		await NwTask.SwitchToMainThread();

		_currentSnapshot = snapshot;
		_productRows.Clear();
		_selectedProductIndex = -1;
		_selectedProductQuantityAvailable = 0;
		_selectedProductUnitPrice = 0;
		_currentGoldOnHand = snapshot.Buyer.GoldOnHand;

		Token().SetBindValue(View.StallTitle, snapshot.Summary.StallName);

		bool descriptionVisible = !string.IsNullOrWhiteSpace(snapshot.Summary.Description);
		Token().SetBindValue(View.StallDescriptionVisible, descriptionVisible);
		Token().SetBindValue(View.StallDescription, descriptionVisible ? snapshot.Summary.Description! : string.Empty);

		bool noticeVisible = !string.IsNullOrWhiteSpace(snapshot.Summary.Notice);
		Token().SetBindValue(View.StallNoticeVisible, noticeVisible);
		Token().SetBindValue(View.StallNotice, noticeVisible ? snapshot.Summary.Notice! : string.Empty);

		Token().SetBindValue(View.GoldText, FormatGold(snapshot.Buyer.GoldOnHand));

		ApplyFeedback(snapshot.FeedbackVisible, snapshot.FeedbackMessage, snapshot.FeedbackColor ?? ColorConstants.White);

		// Initialize preview as hidden
		Token().SetBindValue(View.PreviewVisible, false);
		Token().SetBindValue(View.PreviewPlaceholderVisible, true);
		Token().SetBindValue(View.QuantityRowVisible, false);

		// Initialize filter controls and set up watches
		InitializeFilterOptions(snapshot.Products);
		SetupFilterWatches();

		RefreshProductList();
	}

	private async Task HandlePurchaseAsync(int rowIndex)
	{
		if (_isProcessing || _isClosing)
		{
			return;
		}

		if (rowIndex < 0 || rowIndex >= _productRows.Count)
		{
			return;
		}

		if (_sessionId is not Guid sessionId)
		{
			return;
		}

		ProductRow row = _productRows[rowIndex];
		if (!row.CanPurchase)
		{
			return;
		}

		_isProcessing = true;

		try
		{
			// Parse quantity from the text field, defaulting to 1 if invalid
			int quantity = 1;
			string quantityText = Token().GetBindValue(View.QuantityValue);
			if (!string.IsNullOrWhiteSpace(quantityText) && int.TryParse(quantityText.Trim(), out int parsedQuantity))
			{
				quantity = Math.Max(1, parsedQuantity);
			}

			// Clamp to available quantity
			if (quantity > _selectedProductQuantityAvailable)
			{
				quantity = _selectedProductQuantityAvailable;
			}

			PlayerStallPurchaseRequest request = new(
				sessionId,
				_config.StallId,
				row.ProductId,
				_config.BuyerPersona,
				quantity);

			PlayerStallPurchaseResult result = await EventManager
				.RequestPurchaseAsync(request)
				.ConfigureAwait(false);

			await HandlePurchaseResultAsync(result).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error while processing stall purchase request for stall {StallId}.", _config.StallId);
			await HandlePurchaseResultAsync(PlayerStallPurchaseResult.Fail(
				"We couldn't complete that purchase.",
				ColorConstants.Red)).ConfigureAwait(false);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task HandlePurchaseResultAsync(PlayerStallPurchaseResult result)
	{
		Color color = result.MessageColor ?? (result.Success ? ColorConstants.Lime : ColorConstants.Orange);
		await NwTask.SwitchToMainThread();
		ApplyFeedback(!string.IsNullOrWhiteSpace(result.Message), result.Message, color);

		if (result.UpdatedSnapshot is not null)
		{
			await ApplySnapshotAsync(result.UpdatedSnapshot).ConfigureAwait(false);
		}
	}

	private void ApplyFeedback(bool visible, string? message, Color color)
	{
		Token().SetBindValue(View.FeedbackVisible, visible);
		Token().SetBindValue(View.FeedbackText, visible && !string.IsNullOrWhiteSpace(message) ? message! : string.Empty);
		Token().SetBindValue(View.FeedbackColor, color);
	}

	private void NotifyError(string message)
	{
		if (_player.IsValid)
		{
			_player.SendServerMessage(message, ColorConstants.Red);
		}
	}

	private static string FormatGold(int amount)
	{
		return string.Format(CultureInfo.InvariantCulture, "Gold on hand: {0:n0} gp", Math.Max(0, amount));
	}

	private static string FormatPrice(int amount)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0:n0} gp", Math.Max(0, amount));
	}

	private void HandleSelectProduct(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= _productRows.Count)
		{
			Log.Warn("Invalid row index {RowIndex} for product selection (count: {Count})", rowIndex, _productRows.Count);
			return;
		}

		_selectedProductIndex = rowIndex;
		ProductRow row = _productRows[rowIndex];
		PlayerStallProductView product = row.Product;

		// Track selected product details for quantity calculations
		_selectedProductQuantityAvailable = product.QuantityAvailable;
		_selectedProductUnitPrice = product.Price;

		Log.Debug("Selected product: {ProductName} (ID: {ProductId}, Purchasable: {CanPurchase})",
			product.DisplayName, product.ProductId, row.CanPurchase);

		// Show preview panel
		Token().SetBindValue(View.PreviewVisible, true);
		Token().SetBindValue(View.PreviewPlaceholderVisible, false);

		// Set preview content
		Token().SetBindValue(View.PreviewItemName, product.DisplayName);
		Token().SetBindValue(View.PreviewItemCost, string.Format(CultureInfo.InvariantCulture, "Unit Price: {0}", FormatPrice(product.Price)));

		bool hasDescription = !string.IsNullOrWhiteSpace(product.Tooltip);
		Token().SetBindValue(View.PreviewDescriptionVisible, hasDescription);
		Token().SetBindValue(View.PreviewNoDescriptionVisible, !hasDescription);

		// Format description with item type prefix
		string descriptionText = FormatDescriptionWithItemType(product.Tooltip, product.ItemTypeName);
		Token().SetBindValue(View.PreviewItemDescription, descriptionText);

		Log.Debug("Preview set - HasDescription: {HasDescription}, Description: {Description}",
			hasDescription, hasDescription ? product.Tooltip : "(none)");

		// Show quantity row for stackable items (quantity > 1 available)
		bool isStackable = product.QuantityAvailable > 1;
		Token().SetBindValue(View.QuantityRowVisible, isStackable);
		Token().SetBindValue(View.QuantityValue, "1");
		Token().SetBindValue(View.QuantityMaxLabel, string.Format(CultureInfo.InvariantCulture, "/ {0} max", product.QuantityAvailable));
		Token().SetBindValue(View.TotalCostLabel, string.Format(CultureInfo.InvariantCulture, "Total: {0}", FormatPrice(product.Price)));

		// Enable buy button only if purchasable
		Token().SetBindValue(View.PreviewBuyEnabled, row.CanPurchase);
	}

	private static string FormatDescriptionWithItemType(string? description, string? itemTypeName)
	{
		bool hasItemType = !string.IsNullOrWhiteSpace(itemTypeName);
		bool hasDescription = !string.IsNullOrWhiteSpace(description);

		if (!hasItemType && !hasDescription)
		{
			return string.Empty;
		}

		if (!hasItemType)
		{
			return description!;
		}

		if (!hasDescription)
		{
			return $"[{itemTypeName}]";
		}

		return $"[{itemTypeName}]\n\n{description}";
	}

	private void SetupFilterWatches()
	{
		Token().SetBindWatch(View.SearchFilter, true);
		Token().SetBindWatch(View.BaseItemTypeSelection, true);
		Token().SetBindValue(View.SearchFilter, _searchTerm);
	}

	private void InitializeFilterOptions(IReadOnlyList<PlayerStallProductView> products)
	{
		List<NuiComboEntry> options = [new NuiComboEntry("All Types", -1)];

		// Collect unique base item types present in the current products
		var uniqueTypes = products
			.Where(p => p.BaseItemType.HasValue)
			.Select(p => (Type: p.BaseItemType!.Value, Name: p.ItemTypeName ?? BaseItemTypeNameResolver.GetDisplayName(p.BaseItemType) ?? $"Type {p.BaseItemType}"))
			.DistinctBy(t => t.Type)
			.OrderBy(t => t.Name);

		foreach ((int type, string name) in uniqueTypes)
		{
			options.Add(new NuiComboEntry(name, type));
		}

		Token().SetBindValue(View.BaseItemTypeOptions, options);
		Token().SetBindValue(View.BaseItemTypeSelection, _selectedBaseItemType);
	}

	private void HandleWatchEvent(ModuleEvents.OnNuiEvent eventData)
	{
		if (eventData.ElementId == View.SearchFilter.Key)
		{
			_searchTerm = (Token().GetBindValue(View.SearchFilter) ?? string.Empty).Trim();
			RefreshProductList();
			return;
		}

		if (eventData.ElementId == View.BaseItemTypeSelection.Key)
		{
			_selectedBaseItemType = Token().GetBindValue(View.BaseItemTypeSelection);
			RefreshProductList();
		}
	}

	private IEnumerable<PlayerStallProductView> ApplyFilters(IReadOnlyList<PlayerStallProductView> products)
	{
		IEnumerable<PlayerStallProductView> filtered = products;

		if (!string.IsNullOrEmpty(_searchTerm))
		{
			filtered = filtered.Where(p =>
				p.DisplayName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
				(!string.IsNullOrEmpty(p.OriginalName) && p.OriginalName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)) ||
				(!string.IsNullOrEmpty(p.Tooltip) && p.Tooltip.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)));
		}

		if (_selectedBaseItemType >= 0)
		{
			filtered = filtered.Where(p => p.BaseItemType == _selectedBaseItemType);
		}

		return filtered;
	}

	private void RefreshProductList()
	{
		if (_currentSnapshot is null)
		{
			return;
		}

		_productRows.Clear();
		_selectedProductIndex = -1;

		// Hide preview when list refreshes
		Token().SetBindValue(View.PreviewVisible, false);
		Token().SetBindValue(View.PreviewPlaceholderVisible, true);

		IEnumerable<PlayerStallProductView> filteredProducts = ApplyFilters(_currentSnapshot.Products);

		List<string> entries = new();
		List<string> tooltips = new();
		List<bool> enabled = new();

		foreach (PlayerStallProductView product in filteredProducts)
		{
			bool canPurchase = product.IsPurchasable && !product.IsSoldOut;
			_productRows.Add(new ProductRow(product.ProductId, canPurchase, product));

			string status = product.IsSoldOut
				? "(Sold out)"
				: product.QuantityAvailable > 0
					? string.Format(CultureInfo.InvariantCulture, "({0} on hand)", product.QuantityAvailable)
					: string.Empty;

			string statusSuffix = string.IsNullOrWhiteSpace(status) ? string.Empty : " " + status;

			// Display original name if different from current name
			string originalNameSuffix = string.Empty;
			if (!string.IsNullOrWhiteSpace(product.OriginalName) &&
			    !string.Equals(product.OriginalName, product.DisplayName, StringComparison.OrdinalIgnoreCase))
			{
				originalNameSuffix = string.Format(CultureInfo.InvariantCulture, " (Originally: {0})", product.OriginalName);
			}

			string entry = string.Format(
				CultureInfo.InvariantCulture,
				"{0} - {1}{2}{3}",
				product.DisplayName,
				FormatPrice(product.Price),
				statusSuffix,
				originalNameSuffix);

			entries.Add(entry);
			tooltips.Add($"{product.Price.ToString()} gp");
			enabled.Add(true); // Always allow selecting to see preview
		}

		Token().SetBindValues(View.ProductEntries, entries);
		Token().SetBindValues(View.ProductTooltips, tooltips);
		Token().SetBindValues(View.ProductSelectable, enabled);
		Token().SetBindValue(View.ProductCount, entries.Count);
	}

	private sealed record ProductRow(long ProductId, bool CanPurchase, PlayerStallProductView Product);
}
