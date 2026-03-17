using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Scoped service that holds the shared UI state for the unified World Engine Editor.
/// One instance per Blazor circuit (per browser tab).
/// </summary>
public class WorldEngineEditorState
{
    // ── Endpoint ────────────────────────────────────────────────────
    private Guid? _selectedEndpointId;

    public Guid? SelectedEndpointId
    {
        get => _selectedEndpointId;
        set
        {
            if (_selectedEndpointId == value) return;
            _selectedEndpointId = value;
            OnEndpointChanged?.Invoke();
        }
    }

    public event Action? OnEndpointChanged;

    // ── Active entity type (activity bar selection) ─────────────────
    private WorldEngineEntityType? _activeEntityType;

    public WorldEngineEntityType? ActiveEntityType
    {
        get => _activeEntityType;
        set
        {
            if (_activeEntityType == value) return;
            _activeEntityType = value;
            OnActiveEntityTypeChanged?.Invoke();
        }
    }

    public event Action? OnActiveEntityTypeChanged;

    // ── Tabs ────────────────────────────────────────────────────────
    private readonly List<EditorTab> _openTabs = [];
    private string? _activeTabId;

    public IReadOnlyList<EditorTab> OpenTabs => _openTabs;

    public string? ActiveTabId
    {
        get => _activeTabId;
        set
        {
            if (_activeTabId == value) return;
            _activeTabId = value;
            OnActiveTabChanged?.Invoke();
        }
    }

    public EditorTab? ActiveTab => _openTabs.Find(t => t.Id == _activeTabId);

    public event Action? OnActiveTabChanged;
    public event Action? OnTabListChanged;

    /// <summary>
    /// Open a new tab or activate an existing tab for the same entity.
    /// Returns the tab that was opened or activated.
    /// </summary>
    public EditorTab OpenTab(WorldEngineEntityType entityType, string title, string? entityKey = null)
    {
        // If a tab for this exact entity already exists, just activate it
        EditorTab? existing = entityKey != null
            ? _openTabs.Find(t => t.EntityType == entityType && t.EntityKey == entityKey)
            : null;

        if (existing != null)
        {
            ActiveTabId = existing.Id;
            return existing;
        }

        EditorTab tab = new()
        {
            EntityType = entityType,
            Title = title,
            EntityKey = entityKey,
        };

        _openTabs.Add(tab);
        OnTabListChanged?.Invoke();
        ActiveTabId = tab.Id;
        return tab;
    }

    /// <summary>Close a tab by id. Returns true if the tab existed.</summary>
    public bool CloseTab(string tabId)
    {
        int index = _openTabs.FindIndex(t => t.Id == tabId);
        if (index < 0) return false;

        _openTabs.RemoveAt(index);
        OnTabListChanged?.Invoke();

        // If we closed the active tab, activate the nearest one
        if (_activeTabId == tabId)
        {
            if (_openTabs.Count > 0)
            {
                int nextIndex = Math.Min(index, _openTabs.Count - 1);
                ActiveTabId = _openTabs[nextIndex].Id;
            }
            else
            {
                ActiveTabId = null;
            }
        }

        return true;
    }

    /// <summary>Mark a tab as dirty (has unsaved changes).</summary>
    public void MarkDirty(string tabId, bool dirty = true)
    {
        EditorTab? tab = _openTabs.Find(t => t.Id == tabId);
        if (tab != null && tab.IsDirty != dirty)
        {
            tab.IsDirty = dirty;
            OnTabListChanged?.Invoke();
        }
    }
}
