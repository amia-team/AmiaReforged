using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

[ServiceBinding(typeof(WindowDirector))]
public sealed class WindowDirector : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<NwPlayer, List<IScryPresenter>> _activeWindows = new();

    public WindowDirector()
    {
        NwModule.Instance.OnClientEnter += RegisterPlayer;
        NwModule.Instance.OnClientLeave += PurgeWindows;

        NwModule.Instance.OnNuiEvent += HandleOpenClose;
    }

    private void HandleOpenClose(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Close) return;
        _activeWindows.TryGetValue(obj.Token.Player, out List<IScryPresenter>? playerWindows);
        
        Log.Info("Attempting to remove window for player: " + obj.Token.Player.LoginCreature?.Name);
        IScryPresenter? window = playerWindows?.Find(w => w.Token() == obj.Token);

        if (window != null)
        {
            Log.Info("Window found, removing.");
            playerWindows?.Remove(window);
        }
    }

    private void RegisterPlayer(ModuleEvents.OnClientEnter obj)
    {
        _activeWindows.Add(obj.Player, new List<IScryPresenter>());
    }

    private void PurgeWindows(ModuleEvents.OnClientLeave obj)
    {
        _activeWindows[obj.Player].ForEach(w => w.Close());
        _activeWindows.Remove(obj.Player);
    }

    public void OpenWindow(IScryPresenter window)
    {
        window.Initialize();
        window.Create();
        
        _activeWindows.TryGetValue(window.Token().Player, out List<IScryPresenter>? playerWindows);
        playerWindows?.Add(window);
    }
    
    public void CloseWindow(NwPlayer player, Type type)
    {
        if (!IsWindowOpen(player, type)) return;
            
        _activeWindows.TryGetValue(player, out List<IScryPresenter>? playerWindows);
        
        IScryPresenter? window = playerWindows?.Find(w => w.GetType() == type);
        
        window?.Close();
    }

    public void Dispose()
    {
        foreach (List<IScryPresenter> windows in _activeWindows.Values)
        {
            windows.ForEach(w => w.Close());
        }

        _activeWindows.Clear();
    }

    public bool IsWindowOpen(NwPlayer player, Type type)
    {
        _activeWindows.TryGetValue(player, out List<IScryPresenter>? playerWindows);
        return playerWindows?.Any(w => w.GetType() == type) ?? false;
    }
}