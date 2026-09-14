using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    /// <summary>Sentinel entity key opening the global progression tab.</summary>
    public const string ProgressionSentinel = "__progression__";

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

    private Task OpenNewWorkstationTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.Workstations, "New Workstation", entityKey: null);
        return Task.CompletedTask;
    }

    private Task OpenNewRecipeTemplateTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.RecipeTemplates, "New Recipe Template", entityKey: null);
        return Task.CompletedTask;
    }

    private Task OpenNewIndustryTab()
    {
        EditorState.OpenTab(WorldEngineEntityType.Industries, "New Industry", entityKey: null);
        return Task.CompletedTask;
    }

    private readonly Dictionary<string, IndustryDefinitionDto> _newIndustryDtos = [];

    private IndustryDefinitionDto NewIndustryFor(string tabId)
    {
        if (!_newIndustryDtos.TryGetValue(tabId, out IndustryDefinitionDto? dto))
        {
            dto = new IndustryDefinitionDto();
            _newIndustryDtos[tabId] = dto;
        }
        return dto;
    }

    private async Task HandleIndustrySaved(string tabId, IndustryDefinitionDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Industries)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.Industries, saved.Name, saved.Tag);
        }

        StateHasChanged();
    }

    private async Task HandleIndustryDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Industries)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }

    private readonly Dictionary<string, WorkstationDefinitionDto> _newWorkstationDtos = [];

    private WorkstationDefinitionDto NewWorkstationFor(string tabId)
    {
        if (!_newWorkstationDtos.TryGetValue(tabId, out WorkstationDefinitionDto? dto))
        {
            dto = new WorkstationDefinitionDto();
            _newWorkstationDtos[tabId] = dto;
        }
        return dto;
    }

    private async Task HandleWorkstationSaved(string tabId, WorkstationDefinitionDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Workstations)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.Workstations, saved.Name, saved.Tag);
        }

        StateHasChanged();
    }

    private async Task HandleWorkstationDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.Workstations)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
    }

    private readonly Dictionary<string, RecipeTemplateDefinitionDto> _newRecipeTemplateDtos = [];

    private RecipeTemplateDefinitionDto NewRecipeTemplateFor(string tabId)
    {
        if (!_newRecipeTemplateDtos.TryGetValue(tabId, out RecipeTemplateDefinitionDto? dto))
        {
            dto = new RecipeTemplateDefinitionDto();
            _newRecipeTemplateDtos[tabId] = dto;
        }
        return dto;
    }

    private async Task HandleRecipeTemplateSaved(string tabId, RecipeTemplateDefinitionDto saved)
    {
        EditorTab? tab = EditorState.OpenTabs.FirstOrDefault(t => t.Id == tabId);
        bool wasCreating = tab?.EntityKey is null;

        _tabData[tabId] = saved;
        EditorState.MarkDirty(tabId, false);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.RecipeTemplates)
        {
            await LoadEntityList(reset: true);
        }

        if (wasCreating)
        {
            CloseTab(tabId);
            EditorState.OpenTab(WorldEngineEntityType.RecipeTemplates, saved.Name, saved.Tag);
        }

        StateHasChanged();
    }

    private async Task HandleRecipeTemplateDeleted(string tabId, string tag)
    {
        CloseTab(tabId);

        if (EditorState.ActiveEntityType == WorldEngineEntityType.RecipeTemplates)
        {
            await LoadEntityList(reset: true);
        }

        StateHasChanged();
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
