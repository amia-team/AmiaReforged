using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    // ═══════════════════════════════════════════════════════════════════
    //  Coinhouse Editor tabs (Editors/CoinhouseEditor.razor)
    // ═══════════════════════════════════════════════════════════════════

    private readonly Dictionary<string, CoinhouseDto> _newCoinhouseDtos = [];

    private CoinhouseDto NewCoinhouseFor(string tabId)
    {
        if (!_newCoinhouseDtos.TryGetValue(tabId, out CoinhouseDto? dto))
        {
            dto = new CoinhouseDto { Settlement = 1 };
            _newCoinhouseDtos[tabId] = dto;
        }
        return dto;
    }

    private Task OpenNewCoinhouseTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.Coinhouses, "New Coinhouse", entityKey: null);
        return Task.CompletedTask;
    }

    private async Task HandleCoinhouseSaved(string tabId, CoinhouseDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Coinhouses)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            // Re-key the create tab onto the real tag.
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.Coinhouses, saved.Tag, saved.Tag);
        }

        StateHasChanged();
    }

    private async Task HandleCoinhouseDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Coinhouses)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }
}
