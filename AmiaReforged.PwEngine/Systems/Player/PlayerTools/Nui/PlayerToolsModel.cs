using System.Reflection;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;

public class PlayerToolsModel
{
    public List<IToolWindow> VisibleWindows { get; private set; } = new();
    public bool CharacterIsPersisted { get; set; }

    private readonly NwPlayer _player;
    private string _searchTerm = string.Empty;

    private List<IToolWindow> GetVisibleWindows()
    {
        List<IToolWindow> windows = GetAvailableWindows();
        if (_searchTerm == string.Empty)
        {
            return windows;
        }

        return windows.FindAll(w => w.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
    }


    public PlayerToolsModel(NwPlayer player)
    {
        _player = player;
    }

    private List<IToolWindow> GetAvailableWindows()
    {
        // Get all types of type IToolWindow
        List<IToolWindow> windows = new();
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type interfaceType = typeof(IToolWindow);

        foreach (Type type in assembly.GetTypes()
                     .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false }))
        {
            if (Activator.CreateInstance(type, _player) is IToolWindow { ListInPlayerTools: true } window)
            {
                if(window.RequiresPersistedCharacter && !CharacterIsPersisted)
                {
                    continue;
                }
                
                windows.Add(window);
            }
        }

        return windows;
    }


    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
    }

    public void RefreshWindowList()
    {
        VisibleWindows = GetVisibleWindows();
    }

    public void ClearVisibleWindows()
    {
        VisibleWindows.Clear();
    }
}