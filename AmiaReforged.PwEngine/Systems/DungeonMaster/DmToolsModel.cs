using System.Reflection;
using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NLog;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster;

public class DmToolsModel(NwPlayer player)
{
    public List<IDmWindow> VisibleWindows { get; private set; } = new();
    
    private string _searchTerm = string.Empty;

    public void RefreshWindowList()
    {
        VisibleWindows = GetVisibleWindows();
    }
    
    private List<IDmWindow> GetVisibleWindows()
    {
        List<IDmWindow> windows = GetAvailableWindows();
        if (_searchTerm.IsNullOrEmpty()) return windows;
        
        return windows.FindAll(w => w.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
    }
    
    private List<IDmWindow> GetAvailableWindows()
    {
        // Get all types of type IToolWindow
        List<IDmWindow> windows = new();
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type interfaceType = typeof(IDmWindow);

        foreach (Type type in assembly.GetTypes()
                     .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false }))
        {
            if (Activator.CreateInstance(type, player) is IDmWindow { ListInDmTools: true } window)
            {
                LogManager.GetCurrentClassLogger().Info($"Found window {type.Name}");
                windows.Add(window);
            }
        }

        return windows;
    }

    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
    }
    
    public void ClearVisibleWindows()
    {
        VisibleWindows.Clear();
    }
}