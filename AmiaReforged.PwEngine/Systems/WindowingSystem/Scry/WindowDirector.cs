using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

[ServiceBinding(typeof(WindowDirector))]
public sealed class WindowDirector : IDisposable
{
    private readonly Dictionary<NwPlayer, List<IWindow>> _openedWindows = new();

    public WindowDirector()
    {
        NwModule.Instance.OnClientEnter += RegisterPlayer;
        NwModule.Instance.OnClientLeave += PurgeWindows;

        NwModule.Instance.OnNuiEvent += HandleOpenClose;
    }

    private void HandleOpenClose(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Close) return;
        _openedWindows.TryGetValue(obj.Token.Player, out List<IWindow>? playerWindows);
        IWindow? window = playerWindows?.Find(w => w.GetToken() == obj.Token);

        if (window != null) playerWindows?.Remove(window);
    }

    private void RegisterPlayer(ModuleEvents.OnClientEnter obj)
    {
        _openedWindows.Add(obj.Player, new List<IWindow>());
    }

    private void PurgeWindows(ModuleEvents.OnClientLeave obj)
    {
        _openedWindows[obj.Player].ForEach(w => w.Close());
        _openedWindows.Remove(obj.Player);
    }

    public void OpenWindow(IWindow window)
    {
        window.Init();
        window.Create();
    }
    
    public void CloseWindow(NwPlayer player, Type type)
    {
        if (!IsWindowOpen(player, type)) return;
            
        _openedWindows.TryGetValue(player, out List<IWindow>? playerWindows);
        
        IWindow? window = playerWindows?.Find(w => w.GetType() == type);
        
        window?.Close();
    }

    public void Dispose()
    {
        foreach (List<IWindow> windows in _openedWindows.Values)
        {
            windows.ForEach(w => w.Close());
        }

        _openedWindows.Clear();
    }

    public bool IsWindowOpen(NwPlayer player, Type type)
    {
        _openedWindows.TryGetValue(player, out List<IWindow>? playerWindows);
        return playerWindows?.Any(w => w.GetType() == type) ?? false;
    }
}