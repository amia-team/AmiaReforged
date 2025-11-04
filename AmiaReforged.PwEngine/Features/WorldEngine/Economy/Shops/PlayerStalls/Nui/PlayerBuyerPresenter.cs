using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class PlayerBuyerPresenter : ScryPresenter<PlayerBuyerView>
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
			Geometry = new NuiRect(80f, 80f, 580f, 460f),
			Resizable = false
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
		if (eventData.EventType != NuiEventType.Click)
		{
			return;
		}

		if (eventData.ElementId == View.BuyButton.Id)
		{
			int rowIndex = eventData.ArrayIndex;
			_ = HandlePurchaseAsync(rowIndex);
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

		_productRows.Clear();

		Token().SetBindValue(View.StallTitle, snapshot.Summary.StallName);

		bool descriptionVisible = !string.IsNullOrWhiteSpace(snapshot.Summary.Description);
		Token().SetBindValue(View.StallDescriptionVisible, descriptionVisible);
		Token().SetBindValue(View.StallDescription, descriptionVisible ? snapshot.Summary.Description! : string.Empty);

		bool noticeVisible = !string.IsNullOrWhiteSpace(snapshot.Summary.Notice);
		Token().SetBindValue(View.StallNoticeVisible, noticeVisible);
		Token().SetBindValue(View.StallNotice, noticeVisible ? snapshot.Summary.Notice! : string.Empty);

		Token().SetBindValue(View.GoldText, FormatGold(snapshot.Buyer.GoldOnHand));

		ApplyFeedback(snapshot.FeedbackVisible, snapshot.FeedbackMessage, snapshot.FeedbackColor ?? ColorConstants.White);

	List<string> entries = new(snapshot.Products.Count);
	List<string> tooltips = new(snapshot.Products.Count);
	List<bool> enabled = new(snapshot.Products.Count);

		foreach (PlayerStallProductView product in snapshot.Products)
		{
			bool canPurchase = product.IsPurchasable && !product.IsSoldOut;
			_productRows.Add(new ProductRow(product.ProductId, canPurchase));

			string status = product.IsSoldOut
				? "(Sold out)"
				: product.QuantityAvailable > 0
					? string.Format(CultureInfo.InvariantCulture, "({0} on hand)", product.QuantityAvailable)
					: string.Empty;

			string statusSuffix = string.IsNullOrWhiteSpace(status) ? string.Empty : " " + status;
			string entry = string.Format(
				CultureInfo.InvariantCulture,
				"{0} - {1}{2}",
				product.DisplayName,
				FormatPrice(product.Price),
				statusSuffix);

			entries.Add(entry);
			tooltips.Add(string.IsNullOrWhiteSpace(product.Tooltip) ? string.Empty : product.Tooltip!);
			enabled.Add(canPurchase);
		}

		Token().SetBindValues(View.ProductEntries, entries);
		Token().SetBindValues(View.ProductTooltips, tooltips);
		Token().SetBindValues(View.ProductPurchasable, enabled);
		Token().SetBindValue(View.ProductCount, entries.Count);
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
			PlayerStallPurchaseRequest request = new(
				sessionId,
				_config.StallId,
				row.ProductId,
				_config.BuyerPersona);

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

	private sealed record ProductRow(long ProductId, bool CanPurchase);
}
