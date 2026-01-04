using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Nui;

public sealed class RentPropertyWindowView : ScryView<RentPropertyWindowPresenter>
{
    private readonly RentPropertyWindowConfig _config;

    public readonly NuiBind<string> PropertyName = new("rent_property_name");
    public readonly NuiBind<string> SettlementName = new("rent_property_settlement");
    public readonly NuiBind<bool> SettlementVisible = new("rent_property_settlement_visible");
    public readonly NuiBind<string> PropertyDescription = new("rent_property_description");
    public readonly NuiBind<string> RentCostText = new("rent_property_cost");
    public readonly NuiBind<string> CountdownText = new("rent_property_countdown");
    public readonly NuiBind<string> FeedbackText = new("rent_property_feedback");
    public readonly NuiBind<Color> FeedbackColor = new("rent_property_feedback_color");
    public readonly NuiBind<bool> FeedbackVisible = new("rent_property_feedback_visible");
    public readonly NuiBind<bool> DirectOptionVisible = new("rent_property_direct_visible");
    public readonly NuiBind<bool> DirectOptionEnabled = new("rent_property_direct_enabled");
    public readonly NuiBind<string> DirectOptionStatus = new("rent_property_direct_status");
    public readonly NuiBind<bool> CoinhouseOptionVisible = new("rent_property_coin_visible");
    public readonly NuiBind<bool> CoinhouseOptionEnabled = new("rent_property_coin_enabled");
    public readonly NuiBind<string> CoinhouseOptionStatus = new("rent_property_coin_status");

    public NuiButton DirectButton = null!;
    public NuiButton CoinhouseButton = null!;
    public NuiButton CancelButton = null!;

    public RentPropertyWindowView(NwPlayer player, RentPropertyWindowConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Presenter = new RentPropertyWindowPresenter(this, player, config);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override RentPropertyWindowPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel(PropertyName)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            Height = 26f,
                            Width = 400f
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = SettlementVisible,
                    Children =
                    [
                        new NuiLabel(SettlementName)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            Height = 20f,
                            Width = 400f
                        }
                    ]
                },
                new NuiSpacer { Height = 6f },
                new NuiGroup
                {
                    Height = 110f,
                    Element = new NuiText(PropertyDescription)
                    {
                        Scrollbars = NuiScrollbars.Y
                    }
                },
                new NuiSpacer { Height = 8f },
                new NuiLabel(RentCostText)
                {
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    Height = 20f
                },
                new NuiLabel(CountdownText)
                {
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    Height = 20f
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Visible = DirectOptionVisible,
                    Children =
                    [
                        new NuiButton(_config.DirectOption?.ButtonLabel ?? "Pay from Pockets")
                        {
                            Id = "rent_pay_direct",
                            Width = 210f,
                            Height = 34f,
                            Enabled = DirectOptionEnabled
                        }.Assign(out DirectButton)
                    ]
                },
                new NuiLabel(DirectOptionStatus)
                {
                    Visible = DirectOptionVisible,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    Height = 20f
                },
                new NuiSpacer { Height = 6f },
                new NuiRow
                {
                    Visible = CoinhouseOptionVisible,
                    Children =
                    [
                        new NuiButton(_config.CoinhouseOption?.ButtonLabel ?? "Pay from Coinhouse")
                        {
                            Id = "rent_pay_coinhouse",
                            Width = 210f,
                            Height = 34f,
                            Enabled = CoinhouseOptionEnabled
                        }.Assign(out CoinhouseButton)
                    ]
                },
                new NuiLabel(CoinhouseOptionStatus)
                {
                    Visible = CoinhouseOptionVisible,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    Height = 20f
                },
                new NuiSpacer { Height = 12f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Cancel")
                        {
                            Id = "rent_cancel",
                            Width = 120f,
                            Height = 32f
                        }.Assign(out CancelButton)
                    ]
                },
                new NuiSpacer { Height = 6f },
                new NuiLabel(FeedbackText)
                {
                    Visible = FeedbackVisible,
                    ForegroundColor = FeedbackColor,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    Height = 24f
                }
            ]
        };

        return root;
    }
}

public sealed class RentPropertyWindowPresenter : ScryPresenter<RentPropertyWindowView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly RentPropertyWindowConfig _config;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private CancellationTokenSource? _timeoutCts;
    private bool _isProcessing;
    private bool _isClosing;
    private bool _timeoutTriggered;
    private RentPropertyPaymentOptionViewModel? _directOption;
    private RentPropertyPaymentOptionViewModel? _coinhouseOption;

    public RentPropertyWindowPresenter(RentPropertyWindowView view, NwPlayer player, RentPropertyWindowConfig config)
    {
        View = view;
        _player = player;
        _config = config;
        _directOption = config.DirectOption;
        _coinhouseOption = config.CoinhouseOption;
    }

    public override RentPropertyWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _config.Title)
        {
            Geometry = new NuiRect(0f, 0f, 420f, 420f),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage(
                message: "The rental window could not be initialized. Please notify a DM if this persists.",
                ColorConstants.Red);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage(
                message: "Failed to open the rental agreement window.",
                ColorConstants.Red);
            return;
        }

        try
        {
            InitializeBindings();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize bindings for rent window for player {PlayerName}.", _player.PlayerName);
            _player.SendServerMessage(
                message: "The rental window encountered an error while loading.",
                ColorConstants.Red);
        }

        _timeoutCts = new CancellationTokenSource();
        _ = RunCountdownAsync(_timeoutCts.Token);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click)
        {
            return;
        }

        if (eventData.ElementId == View.DirectButton.Id)
        {
            _ = HandleConfirmAsync(RentalPaymentMethod.OutOfPocket);
            return;
        }

        if (eventData.ElementId == View.CoinhouseButton.Id)
        {
            _ = HandleConfirmAsync(RentalPaymentMethod.CoinhouseAccount);
            return;
        }

        if (eventData.ElementId == View.CancelButton.Id)
        {
            _ = HandleCancelAsync();
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
            _timeoutCts?.Cancel();
            _timeoutCts?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to dispose rental window timeout token cleanly.");
        }
        finally
        {
            _timeoutCts = null;
        }

        if (_config.OnClosed is not null)
        {
            _ = SafeInvokeAsync(_config.OnClosed, nameof(_config.OnClosed));
        }

        try
        {
            _token.Close();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Rent window token close threw an exception for player {PlayerName}.", _player.PlayerName);
        }
    }

    private void InitializeBindings()
    {
        Token().SetBindValue(View.PropertyName, _config.PropertyName);
        Token().SetBindValue(View.PropertyDescription, _config.PropertyDescription);

        bool settlementVisible = !string.IsNullOrWhiteSpace(_config.SettlementName);
        Token().SetBindValue(View.SettlementVisible, settlementVisible);
        Token().SetBindValue(View.SettlementName, settlementVisible ? _config.SettlementName! : string.Empty);

        Token().SetBindValue(View.RentCostText, _config.RentCostText);
        Token().SetBindValue(View.CountdownText, FormatCountdown(_config.Timeout));
        Token().SetBindValue(View.FeedbackVisible, false);
        Token().SetBindValue(View.FeedbackText, string.Empty);
        Token().SetBindValue(View.FeedbackColor, ColorConstants.White);

        ApplyOptionViewModel(_directOption, isDirect: true);
        ApplyOptionViewModel(_coinhouseOption, isDirect: false);
    }

    private async Task HandleConfirmAsync(RentalPaymentMethod method)
    {
        if (_isProcessing || _isClosing)
        {
            return;
        }

        if (!_player.IsValid)
        {
            return;
        }

        _isProcessing = true;

        try
        {
            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.FeedbackVisible, false);
            Token().SetBindValue(View.DirectOptionEnabled, false);
            Token().SetBindValue(View.CoinhouseOptionEnabled, false);

            RentPropertySubmissionResult result;
            try
            {
                result = await _config.OnConfirm(method);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Rental confirmation callback threw for player {PlayerName}.", _player.PlayerName);
                result = RentPropertySubmissionResult.Error(
                    "We couldn't process that selection. Please try again.");
            }

            if (result.DirectOptionUpdate is not null)
            {
                _directOption = result.DirectOptionUpdate;
            }

            if (result.CoinhouseOptionUpdate is not null)
            {
                _coinhouseOption = result.CoinhouseOptionUpdate;
            }

            await UpdateFeedbackAsync(result);

            if (result.Success && result.CloseWindow)
            {
                await NwTask.SwitchToMainThread();
                _token.Close();
                return;
            }

            await ApplyOptionViewModelAsync(_directOption, isDirect: true);
            await ApplyOptionViewModelAsync(_coinhouseOption, isDirect: false);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task HandleCancelAsync()
    {
        if (_isClosing)
        {
            return;
        }

        if (_config.OnCancel is not null)
        {
            await SafeInvokeAsync(_config.OnCancel, nameof(_config.OnCancel));
        }

        await NwTask.SwitchToMainThread();
        _token.Close();
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        try
        {
            TimeSpan remaining = _config.Timeout;
            while (remaining >= TimeSpan.Zero && !token.IsCancellationRequested && !_isClosing)
            {
                await SetCountdownTextAsync(remaining);

                if (remaining <= TimeSpan.Zero)
                {
                    await HandleTimeoutAsync();
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), token);
                remaining -= TimeSpan.FromSeconds(1);
            }
        }
        catch (TaskCanceledException)
        {
            // Window closed by user.
        }
    }

    private async Task HandleTimeoutAsync()
    {
        if (_timeoutTriggered || _isClosing)
        {
            return;
        }

        _timeoutTriggered = true;

        if (_config.OnTimeout is not null)
        {
            await SafeInvokeAsync(_config.OnTimeout, nameof(_config.OnTimeout));
        }

        await NwTask.SwitchToMainThread();
        if (!_isClosing)
        {
            _token.Close();
        }
    }

    private void ApplyOptionViewModel(RentPropertyPaymentOptionViewModel? option, bool isDirect)
    {
        if (isDirect)
        {
            bool visible = option is not null && option.Visible;
            bool enabled = visible && option!.Enabled;
            Token().SetBindValue(View.DirectOptionVisible, visible);
            Token().SetBindValue(View.DirectOptionEnabled, enabled);
            Token().SetBindValue(View.DirectOptionStatus, visible ? option!.StatusText : string.Empty);
        }
        else
        {
            bool visible = option is not null && option.Visible;
            bool enabled = visible && option!.Enabled;
            Token().SetBindValue(View.CoinhouseOptionVisible, visible);
            Token().SetBindValue(View.CoinhouseOptionEnabled, enabled);
            Token().SetBindValue(View.CoinhouseOptionStatus, visible ? option!.StatusText : string.Empty);
        }
    }

    private async Task ApplyOptionViewModelAsync(RentPropertyPaymentOptionViewModel? option, bool isDirect)
    {
        await NwTask.SwitchToMainThread();
        ApplyOptionViewModel(option, isDirect);
    }

    private async Task UpdateFeedbackAsync(RentPropertySubmissionResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Message))
        {
            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.FeedbackVisible, false);
            Token().SetBindValue(View.FeedbackText, string.Empty);
            return;
        }

        await NwTask.SwitchToMainThread();
        Token().SetBindValue(View.FeedbackVisible, true);
        Token().SetBindValue(View.FeedbackText, result.Message);
        Token().SetBindValue(View.FeedbackColor, ResolveFeedbackColor(result.FeedbackKind));
    }

    private async Task SetCountdownTextAsync(TimeSpan remaining)
    {
        await NwTask.SwitchToMainThread();
        Token().SetBindValue(View.CountdownText, FormatCountdown(remaining));
    }

    private static string FormatCountdown(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        return $"Offer expires in {remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    private static Color ResolveFeedbackColor(RentPropertyFeedbackKind kind) => kind switch
    {
        RentPropertyFeedbackKind.Success => ColorConstants.Lime,
        RentPropertyFeedbackKind.Error => ColorConstants.Red,
        _ => ColorConstants.White
    };

    private static async Task SafeInvokeAsync(Func<Task> callback, string callbackName)
    {
        try
        {
            await callback();
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Warn(ex, "RentPropertyWindow callback {CallbackName} threw an exception.", callbackName);
        }
    }
}

public sealed record RentPropertyWindowConfig(
    string Title,
    string PropertyName,
    string PropertyDescription,
    string RentCostText,
    TimeSpan Timeout,
    RentPropertyPaymentOptionViewModel? DirectOption,
    RentPropertyPaymentOptionViewModel? CoinhouseOption,
    Func<RentalPaymentMethod, Task<RentPropertySubmissionResult>> OnConfirm)
{
    public string? SettlementName { get; init; }
    public Func<Task>? OnCancel { get; init; }
    public Func<Task>? OnTimeout { get; init; }
    public Func<Task>? OnClosed { get; init; }
}

public sealed record RentPropertyPaymentOptionViewModel(
    RentalPaymentMethod Method,
    string ButtonLabel,
    bool Visible,
    bool Enabled,
    string StatusText,
    string Tooltip);

public sealed record RentPropertySubmissionResult(
    bool Success,
    string Message,
    RentPropertyFeedbackKind FeedbackKind,
    bool CloseWindow,
    RentPropertyPaymentOptionViewModel? DirectOptionUpdate = null,
    RentPropertyPaymentOptionViewModel? CoinhouseOptionUpdate = null)
{
    public static RentPropertySubmissionResult SuccessResult(
        string message,
        bool closeWindow = true,
        RentPropertyPaymentOptionViewModel? directOptionUpdate = null,
        RentPropertyPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(true, message, RentPropertyFeedbackKind.Success, closeWindow, directOptionUpdate, coinhouseOptionUpdate);

    public static RentPropertySubmissionResult Error(
        string message,
        bool closeWindow = false,
        RentPropertyPaymentOptionViewModel? directOptionUpdate = null,
        RentPropertyPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(false, message, RentPropertyFeedbackKind.Error, closeWindow, directOptionUpdate, coinhouseOptionUpdate);

    public static RentPropertySubmissionResult Info(
        string message,
        bool closeWindow = false,
        RentPropertyPaymentOptionViewModel? directOptionUpdate = null,
        RentPropertyPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(false, message, RentPropertyFeedbackKind.Info, closeWindow, directOptionUpdate, coinhouseOptionUpdate);
}

public enum RentPropertyFeedbackKind
{
    Info,
    Success,
    Error
}
