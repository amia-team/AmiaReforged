using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.LanguageTool;

public class LanguageConfirmationPresenter : ScryPresenter<LanguageConfirmationView>
{
    private readonly NwPlayer _player;
    private readonly Action _onConfirm;
    private readonly string _message;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public LanguageConfirmationPresenter(LanguageConfirmationView view, NwPlayer player, Action onConfirm, string message)
    {
        View = view;
        _player = player;
        _onConfirm = onConfirm;
        _message = message;
    }

    public override LanguageConfirmationView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Confirm Language Selection")
        {
            Geometry = new NuiRect(300, 200, 430f, 360f),
            Resizable = false,
            Border = true
        };
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent click)
    {
        if (click.ElementId == View.ConfirmButton.Id)
        {
            Close();
            _onConfirm.Invoke();
            return;
        }

        if (click.ElementId == View.CancelButton.Id)
        {
            Close();
        }
    }

    public override void UpdateView()
    {
        Token().SetBindValue(View.ConfirmationMessage, _message);
    }

    public override void Create()
    {
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                "The confirmation window could not be created.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Subscribe to events directly
        Token().OnNuiEvent += ProcessEvent;

        UpdateView();
    }

    public override void Close()
    {
        // Unsubscribe from events
        if (_token != null)
        {
            Token().OnNuiEvent -= ProcessEvent;
            _token.Close();
        }
    }
}
