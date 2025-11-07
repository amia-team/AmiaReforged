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
    public HashSet<int> EnabledWindowIndices { get; private set; } = new();
    public Dictionary<int, string> DisabledReasons { get; private set; } = new();
    public bool CharacterIsPersisted { get; set; }

    private List<IToolWindow> GetVisibleWindows()
    {
        List<IToolWindow> windows = GetAvailableWindows();
        if (_searchTerm.IsNullOrEmpty()) return windows;

        return windows.FindAll(w => w.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private List<IToolWindow> GetAvailableWindows()
    {
        List<(IToolWindow window, bool isEnabled, string disabledReason)> windowsWithStatus = new();
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type interfaceType = typeof(IToolWindow);

        foreach (Type type in assembly.GetTypes()
                     .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false }))
        {
            if (Activator.CreateInstance(type, _player) is not IToolWindow window)
            {
                continue;
            }

            // Skip tools that explicitly don't want to be listed
            if (!window.ListInPlayerTools)
                continue;

            // Add all tools but track which ones are enabled
            bool isEnabled = true;
            string disabledReason = string.Empty;

            if (window.RequiresPersistedCharacter && !CharacterIsPersisted)
            {
                isEnabled = false;
                disabledReason = "You must complete character creation first.";
            }
            else if (!window.ShouldListForPlayer(_player))
            {
                isEnabled = false;
                string customReason = window.GetDisabledReason(_player);
                disabledReason = !string.IsNullOrEmpty(customReason)
                    ? customReason
                    : "This tool is not available to your character.";
            }

            windowsWithStatus.Add((window, isEnabled, disabledReason));
        }

        // Sort by title
        var sortedWindows = windowsWithStatus
            .OrderBy(w => w.window.Title)
            .ToList();

        // Build the final list and enabled indices
        List<IToolWindow> windows = new();
        EnabledWindowIndices.Clear();
        DisabledReasons.Clear();

        for (int i = 0; i < sortedWindows.Count; i++)
        {
            windows.Add(sortedWindows[i].window);
            if (sortedWindows[i].isEnabled)
                EnabledWindowIndices.Add(i);
            else
                DisabledReasons[i] = sortedWindows[i].disabledReason;
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
