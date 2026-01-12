using System;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
/// Builder interface for creating confirmation popup windows with Confirm/Cancel buttons.
/// </summary>
public interface IConfirmationPopupBuilder
{
    /// <summary>
    /// Sets the player to whom the popup window will be displayed.
    /// </summary>
    IConfirmationPopupPlayerStage WithPlayer(Anvil.API.NwPlayer player);
}

/// <summary>
/// Stage for setting the title after player is set.
/// </summary>
public interface IConfirmationPopupPlayerStage
{
    /// <summary>
    /// Sets the title of the popup window.
    /// </summary>
    IConfirmationPopupTitleStage WithTitle(string title);
}

/// <summary>
/// Stage for setting the message after title is set.
/// </summary>
public interface IConfirmationPopupTitleStage
{
    /// <summary>
    /// Sets the message of the popup window.
    /// </summary>
    IConfirmationPopupMessageStage WithMessage(string message);
}

/// <summary>
/// Stage for setting callbacks after message is set.
/// </summary>
public interface IConfirmationPopupMessageStage
{
    /// <summary>
    /// Sets the callback to invoke when the user confirms.
    /// </summary>
    IConfirmationPopupConfirmStage OnConfirm(Action onConfirm);
}

/// <summary>
/// Stage for setting the cancel callback.
/// </summary>
public interface IConfirmationPopupConfirmStage
{
    /// <summary>
    /// Sets the callback to invoke when the user cancels.
    /// </summary>
    IConfirmationPopupOpenStage OnCancel(Action onCancel);
}

/// <summary>
/// Final stage for opening the popup.
/// </summary>
public interface IConfirmationPopupOpenStage
{
    /// <summary>
    /// Opens the confirmation popup window.
    /// </summary>
    void Open();
}
