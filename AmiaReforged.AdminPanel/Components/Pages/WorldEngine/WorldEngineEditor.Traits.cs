using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    // ═══════════════════════════════════════════════════════════════════
    //  Trait Editor tabs (Editors/TraitEditor.razor)
    // ═══════════════════════════════════════════════════════════════════

    private readonly Dictionary<string, TraitDefinitionDto> _newTraitDtos = [];

    private TraitDefinitionDto NewTraitFor(string tabId)
    {
        if (!_newTraitDtos.TryGetValue(tabId, out TraitDefinitionDto? dto))
        {
            dto = new TraitDefinitionDto();
            _newTraitDtos[tabId] = dto;
        }
        return dto;
    }

    private Task OpenNewTraitTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.Traits, "New Trait", entityKey: null);
        return Task.CompletedTask;
    }

    private async Task HandleTraitSaved(string tabId, TraitDefinitionDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Traits)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            // Re-key the create tab onto the real tag.
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.Traits, saved.Tag, saved.Tag);
        }

        StateHasChanged();
    }

    private async Task HandleTraitDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Traits)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }
}
