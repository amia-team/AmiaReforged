using System.Reflection;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;

public class PlayerToolsModel
{
    private readonly NwPlayer _player;
    private string _searchTerm = string.Empty;


    public PlayerToolsModel(NwPlayer player)
    {
        _player = player;
    }

    public List<IToolWindow> VisibleWindows { get; private set; } = new();
    public bool CharacterIsPersisted { get; set; }

    private List<IToolWindow> GetVisibleWindows()
    {
        List<IToolWindow> windows = GetAvailableWindows();
        if (_searchTerm.IsNullOrEmpty()) return windows;

        return windows.FindAll(w => w.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private List<IToolWindow> GetAvailableWindows()
    {
        List<IToolWindow> windows = new();
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type interfaceType = typeof(IToolWindow);

        foreach (Type type in assembly.GetTypes()
                     .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false }))
        {
            if (Activator.CreateInstance(type, _player) is IToolWindow { ListInPlayerTools: true } window)
            {
                if (window.RequiresPersistedCharacter && !CharacterIsPersisted) continue;

                windows.Add(window);
            }
        }

        return windows
            .OrderBy<IToolWindow, string>(w => w.Title ?? string.Empty)
            .ToList();
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
