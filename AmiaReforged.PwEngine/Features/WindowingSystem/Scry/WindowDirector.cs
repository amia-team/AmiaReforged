using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

[ServiceBinding(typeof(WindowDirector))]
public sealed class WindowDirector : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<NwPlayer, List<IScryPresenter>> _activeWindows = new();
    private readonly Dictionary<NuiWindowToken, List<NuiWindowToken>> _linkedTokens = new();
    private readonly Dictionary<NuiWindowToken, IScryPresenter> _tokens = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="WindowDirector" /> class.
    ///     Registers event handlers for client enter, client leave, and NUI events.
    /// </summary>
    public WindowDirector()
    {
        NwModule.Instance.OnClientEnter += RegisterPlayer;
        NwModule.Instance.OnClientLeave += PurgeWindows;
        NwModule.Instance.OnNuiEvent += HandleNuiEvents;
    }

    /// <summary>
    ///     Disposes the WindowDirector, closing all active windows and clearing resources.
    /// </summary>
    public void Dispose()
    {
        foreach (List<IScryPresenter> windows in _activeWindows.Values)
        {
            windows.ForEach(w => w.Close());
        }

        _activeWindows.Clear();
    }

    /// <summary>
    ///     Passes NUI events to the relevant Presenter. If the event is a close event, the window is removed.
    /// </summary>
    /// <param name="obj">The NUI event object containing details about the event.</param>
    private void HandleNuiEvents(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Close:
                if (obj.EventType != NuiEventType.Close) return;
                _activeWindows.TryGetValue(obj.Token.Player, out List<IScryPresenter>? playerWindows);

                Log.Info("Attempting to remove window for player: " + obj.Token.Player.LoginCreature?.Name);
                IScryPresenter? window = playerWindows?.Find(w => w.Token() == obj.Token);

                if (window != null)
                {
                    Log.Info(message: "Window found, removing.");
                    _tokens.Remove(window.Token());
                    window.Close();
                    playerWindows?.Remove(window);
                }

                break;
            default:
                _tokens.TryGetValue(obj.Token, out IScryPresenter? presenter);
                presenter?.ProcessEvent(obj);
                break;
        }
    }

    /// <summary>
    ///     Registers a player when they enter the module.
    /// </summary>
    /// <param name="obj">The event object containing details about the client enter event.</param>
    private void RegisterPlayer(ModuleEvents.OnClientEnter obj)
    {
        _activeWindows.Add(obj.Player, new List<IScryPresenter>());
    }

    /// <summary>
    ///     Purges windows associated with a player when they leave the module.
    /// </summary>
    /// <param name="obj">The event object containing details about the client leave event.</param>
    private void PurgeWindows(ModuleEvents.OnClientLeave obj)
    {
        _activeWindows[obj.Player].ForEach(w => w.Close());
        _activeWindows.Remove(obj.Player);
    }

    /// <summary>
    ///     Opens a new window and associates it with the player.
    /// </summary>
    /// <param name="window">The window presenter to be opened.</param>
    public void OpenWindow(IScryPresenter window)
    {
        window.InitBefore();
        window.Create();

        _tokens.TryAdd(window.Token(), window);
        _activeWindows.TryGetValue(window.Token().Player, out List<IScryPresenter>? playerWindows);
        _linkedTokens.TryAdd(window.Token(), new List<NuiWindowToken>());

        playerWindows?.Add(window);
        window.Closing += (_, _) => CloseWindow(window.Token().Player, window.GetType());
    }

    /// <summary>
    ///     Closes a window of a specific type for a player.
    /// </summary>
    /// <param name="player">The player whose window is to be closed.</param>
    /// <param name="type">The type of the window to be closed.</param>
    public void CloseWindow(NwPlayer player, Type type)
    {
        if (!IsWindowOpen(player, type)) return;

        _activeWindows.TryGetValue(player, out List<IScryPresenter>? playerWindows);

        IScryPresenter? window = playerWindows?.Find(w => w.GetType() == type);

        if (window != null)
        {
            Log.Info($"Closing {window.Token().Token} for {player.PlayerName}");
            _linkedTokens.TryGetValue(window.Token(), out List<NuiWindowToken>? linkedTokens);

            linkedTokens?.ForEach(t =>
            {
                t.Close();
                _linkedTokens.Remove(t);
            });

            window.Close();
            playerWindows?.Remove(window);
            _tokens.Remove(window.Token());
        }
    }

    /// <summary>
    ///     Checks if a window of a specific type is open for a player.
    /// </summary>
    /// <param name="player">The player to check for open windows.</param>
    /// <param name="type">The type of the window to check.</param>
    /// <returns>True if the window is open, otherwise false.</returns>
    public bool IsWindowOpen(NwPlayer player, Type type)
    {
        _activeWindows.TryGetValue(player, out List<IScryPresenter>? playerWindows);
        return playerWindows?.Any(w => w.GetType() == type) ?? false;
    }


    /// <summary>
    ///     Use <see cref="GenericWindow" /> to build a new window in a more fluent manner.
    /// </summary>
    /// <param name="nwPlayer"></param>
    /// <param name="title">Title of window</param>
    /// <param name="message">Message that shows in message box</param>
    /// <param name="linkedToken">If this popup was opened because of an event from another window, include the token here.</param>
    /// <param name="ignoreButton">Local variable tag that will be set to never open this window again</param>
    public void OpenPopup(NwPlayer nwPlayer, string title, string message, NuiWindowToken linkedToken = default,
        bool ignoreButton = false)
    {
        if (linkedToken != default)
        {
            _linkedTokens.TryGetValue(linkedToken, out List<NuiWindowToken>? linkedTokens);
            linkedTokens?.Add(linkedToken);
        }

        SimplePopupView view = new(nwPlayer, message, title, ignoreButton);
        SimplePopupPresenter presenter = view.Presenter;

        OpenWindow(presenter);
    }

    public void OpenPopup(NwPlayer nwPlayer, string title, string message, bool ignoreButton = false)
    {
        SimplePopupView view = new(nwPlayer, message, title, ignoreButton);
        SimplePopupPresenter presenter = view.Presenter;

        OpenWindow(presenter);
    }

    public void OpenPopupWithReaction(NwPlayer nwPlayer, string title, string message, Action outcome, bool ignoreButton = false)
    {
        SimplePopupView view = new(nwPlayer, outcome, message, title, ignoreButton);
        SimplePopupPresenter presenter = view.Presenter;
        OpenWindow(presenter);
    }
}
