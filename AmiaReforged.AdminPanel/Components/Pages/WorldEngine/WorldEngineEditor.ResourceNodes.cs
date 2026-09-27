using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    // ═══════════════════════════════════════════════════════════════════
    //  Resource Node Editor tabs (Editors/ResourceNodeEditor.razor)
    // ═══════════════════════════════════════════════════════════════════

    private readonly Dictionary<string, ResourceNodeDefinitionDto> _newResourceNodeDtos = [];

    private ResourceNodeDefinitionDto NewResourceNodeFor(string tabId)
    {
        if (!_newResourceNodeDtos.TryGetValue(tabId, out ResourceNodeDefinitionDto? dto))
        {
            dto = new ResourceNodeDefinitionDto { Uses = 50 };
            _newResourceNodeDtos[tabId] = dto;
        }
        return dto;
    }

    private Task OpenNewResourceNodeTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.ResourceNodes, "New Node", entityKey: null);
        return Task.CompletedTask;
    }

    private async Task HandleResourceNodeSaved(string tabId, ResourceNodeDefinitionDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.ResourceNodes)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            // Re-key the create tab onto the real tag.
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.ResourceNodes, saved.Tag ?? "New Node", saved.Tag);
        }

        StateHasChanged();
    }

    private async Task HandleResourceNodeDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.ResourceNodes)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }
}
