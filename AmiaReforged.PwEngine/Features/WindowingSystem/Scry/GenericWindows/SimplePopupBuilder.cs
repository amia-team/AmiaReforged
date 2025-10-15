using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Builder class for creating simple popup windows.
/// </summary>
public class SimplePopupBuilder : ISimplePopupBuilder, IPlayerStage, ITitleStage, IOpenStage
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    [Inject] private Lazy<WindowDirector>? Director { get; set; }

    /// <summary>
    ///     Opens the popup window. Can also set a token for the window to be linked to another window or set up an ignore
    ///     button before finalizing.
    /// </summary>
    public void Open()
    {
        if (Director == null)
        {
            Log.Error(message: "WindowDirector is not injected");
            return;
        }

        // if (_token != default)
        // {
        //     Director.Value.OpenPopup(_nwPlayer, _title, _message, _token, _ignoreTag != string.Empty);
        //     return;
        // }

        Director.Value.OpenPopup(_nwPlayer, _title, _message, _ignoreTag != string.Empty);
    }

    /// <summary>
    ///     Include this if the popup window is opened because of an event from another window. Setting it again will overwrite
    ///     the previous token.
    /// </summary>
    /// <param name="token">The token of the parent window.</param>
    /// <returns>The current <see cref="IOpenStage" /> instance.</returns>
    public void OpenWithParent(NuiWindowToken token)
    {
        _token = token;
        if (Director == null)
        {
            Log.Error(message: "WindowDirector is not injected");
            return;
        }

        Director.Value.OpenPopup(_nwPlayer, _title, _message, _token, _ignoreTag != string.Empty);
    }

    /// <summary>
    ///     Include this if the popup window should have an ignore button. Will need to separately check the ignore logic
    ///     in the code that opens this popup. Setting it again will overwrite the previous ignore tag.
    /// </summary>
    /// <param name="ignoreTag">The tag for the local int that will be stored to the character's PC Key.</param>
    /// <returns>The current <see cref="IOpenStage" /> instance.</returns>
    public IOpenStage EnableIgnoreButton(string ignoreTag)
    {
        _ignoreTag = ignoreTag;
        return this;
    }

    /// <summary>
    ///     Sets the title of the popup window.
    /// </summary>
    /// <param name="title">The title of the popup window.</param>
    /// <returns>The current <see cref="ITitleStage" /> instance.</returns>
    public ITitleStage WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    ///     Sets the player to whom the popup window will be displayed.
    /// </summary>
    /// <param name="player">The player in question.</param>
    /// <returns>The current <see cref="IPlayerStage" /> instance.</returns>
    public IPlayerStage WithPlayer(NwPlayer player)
    {
        _nwPlayer = player;
        return this;
    }

    /// <summary>
    ///     Sets the message of the popup window.
    /// </summary>
    /// <param name="message">A message that will render in a NuiText field.</param>
    /// <returns>The current <see cref="IOpenStage" /> instance.</returns>
    public IOpenStage WithMessage(string message)
    {
        _message = message;
        return this;
    }

    // N.B.: This is a special case where we build a fluent API on rails, don't instantiate the fields in the constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private NwPlayer _nwPlayer;
    private string _title;
    private string _message;
    private NuiWindowToken _token;
    private string _ignoreTag = string.Empty;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
