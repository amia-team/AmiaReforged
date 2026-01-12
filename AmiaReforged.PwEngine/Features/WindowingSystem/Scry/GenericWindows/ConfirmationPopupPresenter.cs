using Anvil.API;
using Anvil.API.Events;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
/// Presenter for a confirmation popup with Confirm and Cancel buttons.
/// </summary>
public sealed class ConfirmationPopupPresenter : ScryPresenter<ConfirmationPopupView>
{
    private readonly NwPlayer _player;
    private readonly string _title;
    private readonly Action _onConfirm;
    private readonly Action _onCancel;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public ConfirmationPopupPresenter(NwPlayer player, ConfirmationPopupView view, string title, Action onConfirm, Action onCancel)
    {
        _player = player;
        _title = title;
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        View = view;
    }

    public override ConfirmationPopupView View { get; }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;
        
        if (obj.ElementId == ConfirmationPopupView.ConfirmButtonId)
        {
            Close();
            _onConfirm.Invoke();
            return;
        }

        if (obj.ElementId == ConfirmationPopupView.CancelButtonId)
        {
            Close();
            _onCancel.Invoke();
        }
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _title)
        {
            Geometry = new NuiRect(400f, 300f, 450f, 350f),
            Resizable = false
        };
    }

    public override void UpdateView()
    {
        // No updates needed
    }

    public override void Create()
    {
        InitBefore();
        _player.TryCreateNuiWindow(_window!, out _token);
        _token.OnNuiEvent += ProcessEvent;
    }

    public override void Close()
    {
        _token.OnNuiEvent -= ProcessEvent;
        _token.Close();
    }
}
