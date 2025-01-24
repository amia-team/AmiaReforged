using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows
{
    /// <summary>
    /// Provides a static method to create a new instance of <see cref="GenericWindowBuilder"/>.
    /// </summary>
    public static class GenericWindow
    {
        /// <summary>
        /// Creates a new instance of <see cref="GenericWindowBuilder"/>.
        /// </summary>
        /// <returns>A new <see cref="GenericWindowBuilder"/> instance.</returns>
        public static IWindowBuilder Builder()
        {
            return new GenericWindowBuilder();
        }
    }

    /// <summary>
    /// Builder class for creating generic windows.
    /// </summary>
    public class GenericWindowBuilder : IWindowBuilder, IWindowTypeStage
    {
        /// <summary>
        /// Returns the current instance for further configuration.
        /// </summary>
        /// <returns>The current <see cref="IWindowTypeStage"/> instance.</returns>
        public IWindowTypeStage For()
        {
            return this;
        }

        /// <summary>
        /// Creates a new instance of <see cref="SimplePopupBuilder"/> and injects dependencies.
        /// </summary>
        /// <returns>A new <see cref="SimplePopupBuilder"/> instance.</returns>
        public ISimplePopupBuilder SimplePopup()
        {
            InjectionService? service = AnvilCore.GetService<InjectionService>();
            if (service == null)
            {
                LogManager.GetCurrentClassLogger().Error("InjectionService is not available");
                return new SimplePopupBuilder();
            }

            SimplePopupBuilder popupBuilder = service.Inject(new SimplePopupBuilder());
            return popupBuilder;
        }
    }

    /// <summary>
    /// Builder class for creating simple popup windows.
    /// </summary>
    public class SimplePopupBuilder : ISimplePopupBuilder, IPlayerStage, ITitleStage, IOpenStage
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // N.B.: This is a special case where we build a fluent API on rails, don't instantiate the fields in the constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private NwPlayer _nwPlayer;
        private string _title;
        private string _message;
        private NuiWindowToken _token;
        private string _ignoreTag = string.Empty;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Inject] private Lazy<WindowDirector>? Director { get; set; }

        /// <summary>
        /// Sets the player to whom the popup window will be displayed.
        /// </summary>
        /// <param name="player">The player in question.</param>
        /// <returns>The current <see cref="IPlayerStage"/> instance.</returns>
        public IPlayerStage WithPlayer(NwPlayer player)
        {
            _nwPlayer = player;
            return this;
        }

        /// <summary>
        /// Sets the title of the popup window.
        /// </summary>
        /// <param name="title">The title of the popup window.</param>
        /// <returns>The current <see cref="ITitleStage"/> instance.</returns>
        public ITitleStage WithTitle(string title)
        {
            _title = title;
            return this;
        }

        /// <summary>
        /// Sets the message of the popup window.
        /// </summary>
        /// <param name="message">A message that will render in a NuiText field.</param>
        /// <returns>The current <see cref="IOpenStage"/> instance.</returns>
        public IOpenStage WithMessage(string message)
        {
            _message = message;
            return this;
        }

        /// <summary>
        /// Opens the popup window. Can also set a token for the window to be linked to another window or set up an ignore button before finalizing.
        /// </summary>
        public void Open()
        {
            if (Director == null)
            {
                Log.Error("WindowDirector is not injected");
                return;
            }

            if (_token != default)
            {
                Director.Value.OpenPopup(_nwPlayer, _title, _message, _token, _ignoreTag != string.Empty);
                return;
            }

            Director.Value.OpenPopup(_nwPlayer, _title, _message, default, _ignoreTag != string.Empty);
        }

        /// <summary>
        /// Include this if the popup window is opened because of an event from another window. Setting it again will overwrite the previous token.
        /// </summary>
        /// <param name="token">The token of the parent window.</param>
        /// <returns>The current <see cref="IOpenStage"/> instance.</returns>
        public IOpenStage WithToken(NuiWindowToken token)
        {
            _token = token;
            return this;
        }

        /// <summary>
        /// Include this if the popup window should have an ignore button. Will need to separately check the ignore logic
        /// in the code that opens this popup. Setting it again will overwrite the previous ignore tag.
        /// </summary>
        /// <param name="ignoreTag">The tag for the local int that will be stored to the character's PC Key.</param>
        /// <returns>The current <see cref="IOpenStage"/> instance.</returns>
        public IOpenStage EnableIgnoreButton(string ignoreTag)
        {
            _ignoreTag = ignoreTag;
            return this;
        }
    }

    /// <summary>
    /// Interface for building windows.
    /// </summary>
    public interface IWindowBuilder
    {
        /// <summary>
        /// Returns the current instance for further configuration.
        /// </summary>
        /// <returns>The current <see cref="IWindowTypeStage"/> instance.</returns>
        IWindowTypeStage For();
    }

    /// <summary>
    /// Interface for specifying the type of window to build.
    /// </summary>
    public interface IWindowTypeStage
    {
        /// <summary>
        /// Creates a new instance of <see cref="SimplePopupBuilder"/>.
        /// </summary>
        /// <returns>A new <see cref="SimplePopupBuilder"/> instance.</returns>
        ISimplePopupBuilder SimplePopup();
    }

    /// <summary>
    /// Interface for building simple popup windows.
    /// </summary>
    public interface ISimplePopupBuilder
    {
        /// <summary>
        /// Sets the player to whom the popup window will be displayed.
        /// </summary>
        /// <param name="player">The player in question.</param>
        /// <returns>The current <see cref="IPlayerStage"/> instance.</returns>
        IPlayerStage WithPlayer(NwPlayer player);
    }

    /// <summary>
    /// Interface for setting the player of the popup window.
    /// </summary>
    public interface IPlayerStage
    {
        /// <summary>
        /// Sets the title of the popup window.
        /// </summary>
        /// <param name="title">The title of the popup window.</param>
        /// <returns>The current <see cref="ITitleStage"/> instance.</returns>
        ITitleStage WithTitle(string title);
    }

    /// <summary>
    /// Interface for setting the title of the popup window.
    /// </summary>
    public interface ITitleStage
    {
        /// <summary>
        /// Sets the message of the popup window.
        /// </summary>
        /// <param name="message">A message that will render in a NuiText field.</param>
        /// <returns>The current <see cref="IOpenStage"/> instance.</returns>
        IOpenStage WithMessage(string message);
    }

    /// <summary>
    /// Interface for opening the popup window and setting additional options.
    /// </summary>
    public interface IOpenStage
    {
        /// <summary>
        /// Opens the popup window.
        /// </summary>
        void Open();

        /// <summary>
        /// Sets the token for the popup window.
        /// </summary>
        /// <param name="token">The token of the parent window.</param>
        /// <returns>The current <see cref="IOpenStage"/> instance.</returns>
        IOpenStage WithToken(NuiWindowToken token);

        /// <summary>
        /// Enables the ignore button for the popup window.
        /// </summary>
        /// <param name="ignoreTag">The tag for the local int that will be stored to the character's PC Key.</param>
        /// <returns>The current <see cref="IOpenStage"/> instance.</returns>
        IOpenStage EnableIgnoreButton(string ignoreTag);
    }
}