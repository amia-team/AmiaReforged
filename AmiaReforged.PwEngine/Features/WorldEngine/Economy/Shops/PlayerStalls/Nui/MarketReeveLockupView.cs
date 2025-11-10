using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class MarketReeveLockupView : ScryView<MarketReeveLockupPresenter>
{
    private const float WindowW = 580f;
    private const float WindowH = 450f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 5f;

    public readonly NuiBind<string> SummaryText = new("reeve_lockup_summary");
    public readonly NuiBind<int> ItemCount = new("reeve_lockup_item_count");
    public readonly NuiBind<string> ItemEntries = new("reeve_lockup_item_entries");
    public readonly NuiBind<bool> WithdrawAllEnabled = new("reeve_lockup_withdraw_all_enabled");
    public readonly NuiBind<string> FeedbackText = new("reeve_lockup_feedback_text");
    public readonly NuiBind<bool> FeedbackVisible = new("reeve_lockup_feedback_visible");
    public readonly NuiBind<Color> FeedbackColor = new("reeve_lockup_feedback_color");

    public NuiButton WithdrawButton = null!;
    public NuiButton WithdrawAllButton = null!;
    public NuiButton CloseButton = null!;

    public MarketReeveLockupView(NwPlayer player, MarketReeveLockupWindowConfig config)
    {
        Presenter = new MarketReeveLockupPresenter(this, player, config);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override MarketReeveLockupPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiRow headerOverlay = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };

        NuiSpacer headerSpacer = new NuiSpacer { Height = 85f };
        NuiSpacer spacer6 = new NuiSpacer { Height = 6f };
        NuiSpacer spacer8 = new NuiSpacer { Height = 8f };

        List<NuiListTemplateCell> itemTemplate =
        [
            new(new NuiLabel(ItemEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
            {
                Width = 380f
            },
            new(new NuiButton("Withdraw")
            {
                Id = "reeve_lockup_withdraw",
                Width = 110f,
                Height = 26f
            }.Assign(out WithdrawButton))
            {
                Width = 120f,
                VariableSize = false
            }
        ];

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            [
                bgLayer,
                headerOverlay,
                headerSpacer,

                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(SummaryText)
                        {
                            Width = 540f,
                            Height = 24f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiList(itemTemplate, ItemCount)
                        {
                            Width = 540f,
                            RowHeight = 28f,
                            Height = 300f
                        }
                    ]
                },
                spacer8,
                new NuiRow
                {
                    Visible = FeedbackVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(FeedbackText)
                        {
                            Width = 540f,
                            ForegroundColor = FeedbackColor,
                            Height = 22f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Withdraw All")
                        {
                            Id = "reeve_lockup_withdraw_all",
                            Enabled = WithdrawAllEnabled,
                            Width = 140f,
                            Height = 32f
                        }.Assign(out WithdrawAllButton),
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "reeve_lockup_close",
                            Width = 120f,
                            Height = 32f
                        }.Assign(out CloseButton)
                    ]
                }
            ]
        };

        return root;
    }
}

public sealed class MarketReeveLockupPresenter : ScryPresenter<MarketReeveLockupView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly MarketReeveLockupWindowConfig _config;
    private readonly List<LockupRow> _rows = new();

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private bool _isProcessing;
    private bool _isClosing;

    public MarketReeveLockupPresenter(MarketReeveLockupView view, NwPlayer player, MarketReeveLockupWindowConfig config)
    {
        View = view;
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override MarketReeveLockupView View { get; }

    public override NuiWindowToken Token() => _token;

    [Inject] private ReeveLockupService Lockup { get; init; } = null!;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _config.Title)
        {
            Geometry = new NuiRect(120f, 120f, 580f, 450f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            Log.Warn("Market reeve lockup window was not configured correctly.");
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            Log.Warn("Failed to open market reeve lockup window for player {PlayerName}.", _player.PlayerName);
            return;
        }

        foreach (ReeveLockupItemSummary summary in _config.InitialItems)
        {
            _rows.Add(new LockupRow(summary.ItemId, summary.DisplayName, summary.ResRef));
        }

        _ = RefreshAsync();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click)
        {
            return;
        }

        if (eventData.ElementId == View.WithdrawButton.Id)
        {
            _ = WithdrawSingleAsync(eventData.ArrayIndex);
            return;
        }

        if (eventData.ElementId == View.WithdrawAllButton.Id)
        {
            _ = WithdrawAllAsync();
            return;
        }

        if (eventData.ElementId == View.CloseButton.Id)
        {
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
            _token.Close();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Market reeve lockup window close threw for player {PlayerName}.", _player.PlayerName);
        }
    }

    private async Task RefreshAsync()
    {
        await NwTask.SwitchToMainThread();

        int count = _rows.Count;
        string summary = count == 0
            ? "The market reeve is not holding any items for you right now."
            : string.Format(CultureInfo.InvariantCulture, "Items held by the market reeve ({0}).", count);

        List<string> entries = _rows.Select(BuildDisplayText).ToList();

        Token().SetBindValue(View.SummaryText, summary);
        Token().SetBindValues(View.ItemEntries, entries);
        Token().SetBindValue(View.ItemCount, entries.Count);
        Token().SetBindValue(View.WithdrawAllEnabled, entries.Count > 0);
        Token().SetBindValue(View.FeedbackVisible, false);
    }

    private async Task WithdrawSingleAsync(int rowIndex)
    {
        if (_isProcessing || _isClosing)
        {
            return;
        }

        if (rowIndex < 0 || rowIndex >= _rows.Count)
        {
            return;
        }

        await NwTask.SwitchToMainThread();
        NwCreature? recipient = ResolveRecipient();
        if (recipient is null)
        {
            await ShowFeedbackAsync("You must be possessing your character to retrieve items.", ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        _isProcessing = true;
        LockupRow row = _rows[rowIndex];

        try
        {
            IReeveLockupRecipient adapter = new NwReeveLockupRecipient(recipient);

            bool released = await Lockup
                .ReleaseStoredItemAsync(row.StoredItemId, _config.Persona, _config.AreaResRef, adapter)
                .ConfigureAwait(false);

            if (!released)
            {
                await ShowFeedbackAsync("We couldn't retrieve that item right now.", ColorConstants.Red)
                    .ConfigureAwait(false);
                return;
            }

            _rows.RemoveAt(rowIndex);
            await RefreshAsync().ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Recovered {0}.",
                row.DisplayName);

            await ShowFeedbackAsync(message, ColorConstants.Lime).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to release stored item {StoredItemId} for player {PlayerName}.",
                row.StoredItemId,
                _player.PlayerName);
            await ShowFeedbackAsync("We couldn't retrieve that item right now.", ColorConstants.Red)
                .ConfigureAwait(false);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task WithdrawAllAsync()
    {
        if (_isProcessing || _isClosing)
        {
            return;
        }

        if (_rows.Count == 0)
        {
            await ShowFeedbackAsync("There are no items to retrieve.", ColorConstants.Orange).ConfigureAwait(false);
            return;
        }

        await NwTask.SwitchToMainThread();
        NwCreature? recipient = ResolveRecipient();
        if (recipient is null)
        {
            await ShowFeedbackAsync("You must be possessing your character to retrieve items.", ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        _isProcessing = true;

        try
        {
            IReeveLockupRecipient adapter = new NwReeveLockupRecipient(recipient);
            int restored = await Lockup
                .ReleaseInventoryToPlayerAsync(_config.Persona, _config.AreaResRef, adapter)
                .ConfigureAwait(false);

            if (restored <= 0)
            {
                await ShowFeedbackAsync("We couldn't retrieve your stored items right now.", ColorConstants.Red)
                    .ConfigureAwait(false);
                return;
            }

            _rows.Clear();
            await RefreshAsync().ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Recovered {0} stored {1}.",
                restored,
                restored == 1 ? "item" : "items");

            await ShowFeedbackAsync(message, ColorConstants.Lime).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to release all stored items for player {PlayerName}.", _player.PlayerName);
            await ShowFeedbackAsync("We couldn't retrieve your stored items right now.", ColorConstants.Red)
                .ConfigureAwait(false);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private NwCreature? ResolveRecipient()
    {
        if (_player.ControlledCreature is { IsValid: true } controlled)
        {
            return controlled;
        }

        return _config.Recipient is { IsValid: true } fallback ? fallback : null;
    }

    private static string BuildDisplayText(LockupRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.ResRef))
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} [{1}]",
                row.DisplayName,
                row.ResRef);
        }

        return row.DisplayName;
    }

    private async Task ShowFeedbackAsync(string message, Color color)
    {
        await NwTask.SwitchToMainThread();

        Token().SetBindValue(View.FeedbackVisible, true);
        Token().SetBindValue(View.FeedbackText, message);
        Token().SetBindValue(View.FeedbackColor, color);
    }

    private sealed record LockupRow(long StoredItemId, string DisplayName, string? ResRef);
}
