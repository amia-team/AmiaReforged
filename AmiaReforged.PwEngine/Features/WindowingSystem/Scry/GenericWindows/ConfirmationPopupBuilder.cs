using Anvil.API;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
/// Builder class for creating confirmation popup windows with Confirm/Cancel buttons.
/// </summary>
public class ConfirmationPopupBuilder : 
    IConfirmationPopupBuilder, 
    IConfirmationPopupPlayerStage, 
    IConfirmationPopupTitleStage, 
    IConfirmationPopupMessageStage,
    IConfirmationPopupConfirmStage,
    IConfirmationPopupOpenStage
{
    private NwPlayer _player = null!;
    private string _title = string.Empty;
    private string _message = string.Empty;
    private Action _onConfirm = () => { };
    private Action _onCancel = () => { };

    public IConfirmationPopupPlayerStage WithPlayer(NwPlayer player)
    {
        _player = player;
        return this;
    }

    public IConfirmationPopupTitleStage WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public IConfirmationPopupMessageStage WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public IConfirmationPopupConfirmStage OnConfirm(Action onConfirm)
    {
        _onConfirm = onConfirm;
        return this;
    }

    public IConfirmationPopupOpenStage OnCancel(Action onCancel)
    {
        _onCancel = onCancel;
        return this;
    }

    public void Open()
    {
        ConfirmationPopupView view = new(_player, _message, _title, _onConfirm, _onCancel);
        view.Presenter.Create();
    }
}
