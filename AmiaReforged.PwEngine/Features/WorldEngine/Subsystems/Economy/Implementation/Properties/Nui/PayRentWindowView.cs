using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Nui;

public sealed class PayRentWindowView : ScryView<PayRentWindowPresenter>
{
    public readonly NuiBind<string> PropertyName = new("pay_rent_property_name");
    public readonly NuiBind<string> PropertyDescription = new("pay_rent_description");
    public readonly NuiBind<string> RentAmountText = new("pay_rent_amount");
    public readonly NuiBind<string> CurrentDueDateText = new("pay_rent_current_due");
    public readonly NuiBind<string> NewDueDateText = new("pay_rent_new_due");
    public readonly NuiBind<string> FeedbackText = new("pay_rent_feedback");
    public readonly NuiBind<Color> FeedbackColor = new("pay_rent_feedback_color");
    public readonly NuiBind<bool> FeedbackVisible = new("pay_rent_feedback_visible");
    public readonly NuiBind<bool> DirectOptionEnabled = new("pay_rent_direct_enabled");
    public readonly NuiBind<bool> CoinhouseOptionEnabled = new("pay_rent_coin_enabled");
    public readonly NuiBind<string> DirectOptionStatus = new("pay_rent_direct_status");
    public readonly NuiBind<string> CoinhouseOptionStatus = new("pay_rent_coin_status");

    public NuiButton DirectButton = null!;
    public NuiButton CoinhouseButton = null!;
    public NuiButton CancelButton = null!;

    public PayRentWindowView(NwPlayer player, PayRentWindowConfig config)
    {
        Presenter = new PayRentWindowPresenter(this, player, config);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override PayRentWindowPresenter Presenter { get; protected set; }

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
                            Height = 26f,
                            Width = 400f
                        }
                    ]
                },
                new NuiSpacer { Height = 6f },
                new NuiGroup
                {
                    Height = 80f,
                    Element = new NuiText(PropertyDescription) { Scrollbars = NuiScrollbars.Y }
                },
                new NuiSpacer { Height = 8f },
                new NuiLabel(RentAmountText) { Height = 20f },
                new NuiLabel(CurrentDueDateText) { Height = 20f },
                new NuiLabel(NewDueDateText) { Height = 20f },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Visible = FeedbackVisible,
                    Children =
                    [
                        new NuiLabel(FeedbackText)
                        {
                            ForegroundColor = FeedbackColor
                        }
                    ]
                },
                new NuiSpacer { Height = 12f },
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("Pay from Inventory")
                        {
                            Id = "pay_rent_direct_btn",
                            Enabled = DirectOptionEnabled,
                            Width = 200f
                        }.Assign(out DirectButton),
                        new NuiButton("Pay from Coinhouse")
                        {
                            Id = "pay_rent_coin_btn",
                            Enabled = CoinhouseOptionEnabled,
                            Width = 200f
                        }.Assign(out CoinhouseButton)
                    ]
                },
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel(DirectOptionStatus)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            Height = 16f,
                            Width = 200f
                        },
                        new NuiLabel(CoinhouseOptionStatus)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            Height = 16f,
                            Width = 200f
                        }
                    ]
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiButton("Cancel")
                        {
                            Id = "pay_rent_cancel_btn",
                            Width = 120f
                        }.Assign(out CancelButton)
                    ]
                }
            ]
        };

        return root;
    }
}

public sealed class PayRentWindowPresenter : ScryPresenter<PayRentWindowView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly PayRentWindowConfig _config;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private bool _isProcessing;
    private bool _isClosing;

    public PayRentWindowPresenter(PayRentWindowView view, NwPlayer player, PayRentWindowConfig config)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override PayRentWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _config.Title)
        {
            Geometry = new NuiRect(300f, 60f, 420f, 440f),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            InitBefore();
        }

        if (_window is null)
        {
            _player.SendServerMessage("The rent payment window could not be created.", ColorConstants.Red);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Failed to open the rent payment window.", ColorConstants.Red);
            return;
        }

        Token().SetBindValue(View.PropertyName, _config.PropertyName);
        Token().SetBindValue(View.PropertyDescription, _config.PropertyDescription);
        Token().SetBindValue(View.RentAmountText, _config.RentAmountText);
        Token().SetBindValue(View.CurrentDueDateText, _config.CurrentDueDateText);
        Token().SetBindValue(View.NewDueDateText, _config.NewDueDateText);
        Token().SetBindValue(View.FeedbackVisible, false);
        Token().SetBindValue(View.FeedbackText, string.Empty);
        Token().SetBindValue(View.FeedbackColor, ColorConstants.White);

        ApplyOptionViewModel(_config.DirectOption, isDirect: true);
        ApplyOptionViewModel(_config.CoinhouseOption, isDirect: false);
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
        }
        else if (eventData.ElementId == View.CoinhouseButton.Id)
        {
            _ = HandleConfirmAsync(RentalPaymentMethod.CoinhouseAccount);
        }
        else if (eventData.ElementId == View.CancelButton.Id)
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

        if (_config.OnCancel is not null)
        {
            try
            {
                _ = _config.OnCancel();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "PayRentWindow OnCancel callback threw during close.");
            }
        }

        try
        {
            _token.Close();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to close PayRentWindow token cleanly.");
        }
    }

    private async Task HandleConfirmAsync(RentalPaymentMethod method)
    {
        if (_isProcessing || _isClosing || !_player.IsValid)
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

            PayRentSubmissionResult result;
            try
            {
                result = await _config.OnConfirm(method);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Rent payment callback threw for player {PlayerName}.", _player.PlayerName);
                result = PayRentSubmissionResult.Error("Payment processing failed. Please try again.");
            }

            await UpdateFeedbackAsync(result);

            if (result.Success && result.CloseWindow)
            {
                await NwTask.SwitchToMainThread();
                Close();
                return;
            }

            if (result.DirectOptionUpdate is not null)
            {
                await ApplyOptionViewModelAsync(result.DirectOptionUpdate, isDirect: true);
            }

            if (result.CoinhouseOptionUpdate is not null)
            {
                await ApplyOptionViewModelAsync(result.CoinhouseOptionUpdate, isDirect: false);
            }
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
            try
            {
                await _config.OnCancel();
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "PayRentWindow OnCancel callback threw an exception.");
            }
        }

        await NwTask.SwitchToMainThread();
        Close();
    }

    private void ApplyOptionViewModel(PayRentPaymentOptionViewModel? option, bool isDirect)
    {
        if (isDirect)
        {
            bool enabled = option is not null && option.Enabled;
            Token().SetBindValue(View.DirectOptionEnabled, enabled);
            Token().SetBindValue(View.DirectOptionStatus, option?.StatusText ?? string.Empty);
        }
        else
        {
            bool enabled = option is not null && option.Enabled;
            Token().SetBindValue(View.CoinhouseOptionEnabled, enabled);
            Token().SetBindValue(View.CoinhouseOptionStatus, option?.StatusText ?? string.Empty);
        }
    }

    private async Task ApplyOptionViewModelAsync(PayRentPaymentOptionViewModel option, bool isDirect)
    {
        await NwTask.SwitchToMainThread();
        ApplyOptionViewModel(option, isDirect);
    }

    private async Task UpdateFeedbackAsync(PayRentSubmissionResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Message))
        {
            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.FeedbackVisible, false);
            return;
        }

        await NwTask.SwitchToMainThread();
        Token().SetBindValue(View.FeedbackVisible, true);
        Token().SetBindValue(View.FeedbackText, result.Message);
        Token().SetBindValue(View.FeedbackColor, result.FeedbackKind switch
        {
            PayRentFeedbackKind.Success => ColorConstants.Lime,
            PayRentFeedbackKind.Error => ColorConstants.Red,
            PayRentFeedbackKind.Warning => ColorConstants.Orange,
            _ => ColorConstants.White
        });
    }
}

public sealed record PayRentWindowConfig(
    string Title,
    string PropertyName,
    string PropertyDescription,
    string RentAmountText,
    string CurrentDueDateText,
    string NewDueDateText,
    PayRentPaymentOptionViewModel? DirectOption,
    PayRentPaymentOptionViewModel? CoinhouseOption,
    Func<RentalPaymentMethod, Task<PayRentSubmissionResult>> OnConfirm)
{
    public Func<Task>? OnCancel { get; init; }
}

public sealed record PayRentPaymentOptionViewModel(
    RentalPaymentMethod Method,
    string ButtonLabel,
    bool Visible,
    bool Enabled,
    string StatusText,
    string Tooltip);

public sealed record PayRentSubmissionResult(
    bool Success,
    string Message,
    PayRentFeedbackKind FeedbackKind,
    bool CloseWindow,
    PayRentPaymentOptionViewModel? DirectOptionUpdate = null,
    PayRentPaymentOptionViewModel? CoinhouseOptionUpdate = null)
{
    public static PayRentSubmissionResult SuccessResult(string message, bool closeWindow = true) =>
        new(true, message, PayRentFeedbackKind.Success, closeWindow);

    public static PayRentSubmissionResult Error(
        string message,
        bool closeWindow = false,
        PayRentPaymentOptionViewModel? directOptionUpdate = null,
        PayRentPaymentOptionViewModel? coinhouseOptionUpdate = null) =>
        new(false, message, PayRentFeedbackKind.Error, closeWindow, directOptionUpdate, coinhouseOptionUpdate);

    public static PayRentSubmissionResult Warning(string message, bool closeWindow = false) =>
        new(false, message, PayRentFeedbackKind.Warning, closeWindow);
}

public enum PayRentFeedbackKind
{
    Success,
    Error,
    Warning
}
