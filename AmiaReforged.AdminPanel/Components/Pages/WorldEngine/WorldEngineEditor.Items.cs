using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    // ═══════════════════════════════════════════════════════════════════
    //  Item Editor tabs (Editors/ItemEditor.razor)
    // ═══════════════════════════════════════════════════════════════════

    private readonly Dictionary<string, ItemBlueprintDto> _newItemDtos = [];

    private ItemBlueprintDto NewItemFor(string tabId)
    {
        if (!_newItemDtos.TryGetValue(tabId, out ItemBlueprintDto? dto))
        {
            dto = new ItemBlueprintDto { BaseValue = 1, WeightIncreaseConstant = -1 };
            _newItemDtos[tabId] = dto;
        }
        return dto;
    }

    private Task OpenNewItemTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.Items, "New Item", entityKey: null);
        return Task.CompletedTask;
    }

    private async Task HandleItemSaved(string tabId, ItemBlueprintDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Items)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            // Re-key the create tab onto the new tag.
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.Items, saved.Name, saved.ItemTag);
        }

        StateHasChanged();
    }

    private async Task HandleItemDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Items)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }
}
