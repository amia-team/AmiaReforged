using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

[ServiceBinding(typeof(WindowDirector))]
public sealed class WindowDirector : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<NuiWindowToken, IScryPresenter> _tokens = new();
    private readonly Dictionary<NwPlayer, List<IScryPresenter>> _activeWindows = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowDirector"/> class.
    /// Registers event handlers for client enter, client leave, and NUI events.
    /// </summary>
    public WindowDirector()
    {
        NwModule.Instance.OnClientEnter += RegisterPlayer;
        NwModule.Instance.OnClientLeave += PurgeWindows;
        NwModule.Instance.OnNuiEvent += HandleNuiEvents;
    }

    /// <summary>
    /// Passes NUI events to the relevant Presenter. If the event is a close event, the window is removed.
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
                    Log.Info("Window found, removing.");
                    playerWindows?.Remove(window);
                }
                break;
            default:
                _tokens.TryGetValue(obj.Token, out IScryPresenter? presenter);
                presenter?.HandleInput(obj);
                break;
        }
    }

    /// <summary>
    /// Registers a player when they enter the module.
    /// </summary>
    /// <param name="obj">The event object containing details about the client enter event.</param>
    private void RegisterPlayer(ModuleEvents.OnClientEnter obj)
    {
        _activeWindows.Add(obj.Player, new List<IScryPresenter>());
    }

    /// <summary>
    /// Purges windows associated with a player when they leave the module.
    /// </summary>
    /// <param name="obj">The event object containing details about the client leave event.</param>
    private void PurgeWindows(ModuleEvents.OnClientLeave obj)
    {
        _activeWindows[obj.Player].ForEach(w => w.Close());
        _activeWindows.Remove(obj.Player);
    }

    /// <summary>
    /// Opens a new window and associates it with the player.
    /// </summary>
    /// <param name="window">The window presenter to be opened.</param>
    public void OpenWindow(IScryPresenter window)
    {
        window.Initialize();
        window.Create();
        _tokens.Add(window.Token(), window);
        _activeWindows.TryGetValue(window.Token().Player, out List<IScryPresenter>? playerWindows);
        playerWindows?.Add(window);
    }

    /// <summary>
    /// Closes a window of a specific type for a player.
    /// </summary>
    /// <param name="player">The player whose window is to be closed.</param>
    /// <param name="type">The type of the window to be closed.</param>
    public void CloseWindow(NwPlayer player, Type type)
    {
        if (!IsWindowOpen(player, type)) return;

        _activeWindows.TryGetValue(player, out List<IScryPresenter>? playerWindows);

        IScryPresenter? window = playerWindows?.Find(w => w.GetType() == type);

        window?.Close();
        if (window != null)
        {
            playerWindows?.Remove(window);
            _tokens.Remove(window.Token());
        }
    }

    /// <summary>
    /// Disposes the WindowDirector, closing all active windows and clearing resources.
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
    /// Checks if a window of a specific type is open for a player.
    /// </summary>
    /// <param name="player">The player to check for open windows.</param>
    /// <param name="type">The type of the window to check.</param>
    /// <returns>True if the window is open, otherwise false.</returns>
    public bool IsWindowOpen(NwPlayer player, Type type)
    {
        _activeWindows.TryGetValue(player, out List<IScryPresenter>? playerWindows);
        return playerWindows?.Any(w => w.GetType() == type) ?? false;
    }

    public void OpenPopup(NwPlayer nwPlayer, string title, string message)
    {
        SimplePopupView view = new(nwPlayer, message, title);
        SimplePopupPresenter presenter = view.Presenter;
        
        OpenWindow(presenter);
    }
}