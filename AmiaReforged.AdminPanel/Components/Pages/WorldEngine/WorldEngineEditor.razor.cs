using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class WorldEngineEditor
{
    private WorldEngineEditorHostContext _hostContext = null!;
    private List<WorldEngineEndpoint> _endpoints = [];
    private bool _listPanelOpen = true;

    // ── Deploy dialog state ────────────────────────────────────────
    private bool _showDeployDialog;
    private string _deploySourceName = "";
    private WorldEngineEntityType _deployEntityType;
    private string _deployEntityKey = "";

    private bool _canDeploy =>
        EditorState.SelectedEndpointId != null
        && (
            // Standard tab path
            (EditorState.ActiveTab is { EntityKey: not null } tab
             && DeploymentService.SupportedEntityTypes.Contains(tab.EntityType))
            // Interaction editor path (bypasses tabs)
            || (_interactionEditorOpen && !_interactionEditorIsCreating
                && !string.IsNullOrEmpty(_interactionEditorTag))
        );

    // ── Entity list state ───────────────────────────────────────────
    private List<EntityListItem> _listItems = [];
    private bool _listLoading;
    private string? _listError;
    private string _listSearch = "";
    private int _listPage = 1;
    private bool _listHasMore;
    private const int ListPageSize = 50;
    private CancellationTokenSource? _searchCts;

    // ── Tab data state ──────────────────────────────────────────────
    private readonly Dictionary<string, object?> _tabData = new();
    private bool _tabDataLoading;
    private string? _tabDataError;

    // ═══════════════════════════════════════════════════════════════════
    //  Codex Editor — delegated to Editors/CodexEditor.razor
    // ═══════════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════════
    //  Interaction Editor — Script Editor Lifecycle
    // ═══════════════════════════════════════════════════════════════════
    private const string RegionGlInstanceId = "we-region-graph";
    private bool _regionGraphOpen;
    private bool _regionGraphLoading;
    private bool _regionGraphNeedsInit;
    private string? _regionGraphError;
    private int _regionGraphLoadPercent;
    private string _regionGraphLoadPhase = "";

    private AreaGraphDto? _regionGraphData;
    private List<RegionDefinitionDto> _rgRegions = [];
    private List<AreaNodeDto> _rgKnownAreas = [];
    private Dictionary<string, string> _rgRegionColors = new();
    private List<ResourceNodeDefinitionDto> _rgAllResourceDefs = [];

    // Region graph editing state
    private enum RgPanelMode { RegionList, RegionEditor, AreaEditor }
    private RgPanelMode _rgPanelMode = RgPanelMode.RegionList;
    private string _rgRegionListFilter = "";
    private bool _rgIsCreating;
    private RegionDefinitionDto _rgEditRegion = new();
    private bool _rgShowDeleteConfirm;
    private bool _rgShowAddAreaInput;
    private string _rgNewAreaResRef = "";
    private AreaDefinitionDto? _rgEditArea;
    private string? _rgEditAreaRegionTag;
    private string? _rgEditAreaOriginalRegionTag;
    private string? _rgEditAreaName;
    private bool _rgIsSaving;
    private string? _rgError;
    private string? _rgSuccess;
    private string _rgDefTagSearch = "";
    private bool _rgDefTagDropdownOpen;

    // Region graph toolbar state
    private string _rgGraphSearchQuery = "";
    private string _rgSelectedLayout = "cose";

    // Region import/export state
    private bool _rgShowImportModal;
    private string _rgImportJson = "";
    private ImportResult? _rgImportResult;
    private List<JsonElement> _rgImportedRegions = [];
    private bool _rgIsExporting;

    // GL bridge refs
    private IJSObjectReference? _regionBridgeModule;
    private DotNetObjectReference<WorldEngineEditor>? _regionDotNetRef;

    private static readonly JsonSerializerOptions RgCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string[] _rgClimateOptions = { "Tropical", "Arid", "Temperate", "Continental", "Polar", "Marine", "Subterranean", "Planar" };
    private static readonly string[] _rgQualityOptions = { "Terrible", "Poor", "Low", "Average", "Good", "High", "Excellent", "Legendary" };
    private static readonly string[] _rgPoiTypeOptions = { "Undefined", "Dungeon", "Landmark", "ResourceNode", "House", "Guild", "Temple", "Library", "Shop", "Warehouse", "Bank" };

    // ═══════════════════════════════════════════════════════════════════
    //  Interaction Editor (delegated to standalone component)
    // ═══════════════════════════════════════════════════════════════════
    private InteractionEditor? _interactionEditorRef;
    private bool _interactionEditorOpen;
    private bool _interactionEditorIsCreating;
    private string? _interactionEditorTag;
    private List<IndustryDefinitionDto> _interactionEditorIndustries = [];

    // ═══════════════════════════════════════════════════════════════════
    //  Codex Editor state (minimal — full state lives in Editors/CodexEditor.razor)
    // ═══════════════════════════════════════════════════════════════════
    private bool _codexEditorOpen;

    // ── Dialogue editor state ──
    private DialogueTreeEditor? _dialogueEditor;

    protected override void OnInitialized()
    {
        _hostContext = new WorldEngineEditorHostContext
        {
            State = EditorState,
            RefreshEntityListAsync = () => LoadEntityList(reset: true),
            OpenRegionGraphAsync = OpenRegionGraph,
            CloseRegionGraphAsync = CloseRegionGraph,
            IsRegionGraphOpen = () => _regionGraphOpen,
            OpenNewInteractionAsync = OpenNewInteractionEditor,
            OpenNewLoreAsync = () => OpenNewCodexEditor(CodexEditor.CodexSubType.Lore),
            OpenNewQuestAsync = () => OpenNewCodexEditor(CodexEditor.CodexSubType.Quest),
        };

        EditorState.OnEndpointChanged += OnEditorEndpointChanged;
        EditorState.OnActiveEntityTypeChanged += OnActiveEntityTypeChanged;
        EditorState.OnActiveTabChanged += OnActiveTabChanged;
        EditorState.OnTabListChanged += StateHasChangedSafe;
    }

    protected override async Task OnInitializedAsync()
    {
        _endpoints = (await EndpointService.GetEnabledEndpointsAsync()).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Endpoint
    // ═══════════════════════════════════════════════════════════════════

    private void OnEndpointChanged(ChangeEventArgs e)
    {
        string? val = e.Value?.ToString();
        EditorState.SelectedEndpointId = Guid.TryParse(val, out Guid id) ? id : null;
    }

    private void OnEditorEndpointChanged()
    {
        // Push the endpoint selection to ALL API services
        Guid? eid = EditorState.SelectedEndpointId;
        ItemApi.SelectEndpoint(eid);
        ResourceNodeApi.SelectEndpoint(eid);
        RegionApi.SelectEndpoint(eid);
        AreaGraphApi.SelectEndpoint(eid);
        LoreApi.SelectEndpoint(eid);
        QuestApi.SelectEndpoint(eid);
        TraitApi.SelectEndpoint(eid);
        GlyphApi.SelectEndpoint(eid);
        IndustryApi.SelectEndpoint(eid);
        InteractionApi.SelectEndpoint(eid);
        CoinhouseApi.SelectEndpoint(eid);
        DialogueApi.SelectEndpoint(eid);
        OrganizationApi.SelectEndpoint(eid);
        _dialogueEditor?.SelectEndpoint(eid);

        // Reload the current entity list
        _ = InvokeAsync(async () =>
        {
            _listSearch = "";
            await LoadEntityList(reset: true);

            // Pre-load industries for InteractionEditor
            try
            {
                PagedResult<IndustryDefinitionDto> indResult = await IndustryApi.GetAllAsync(null, 1, 200);
                _interactionEditorIndustries = indResult.Items;
            }
            catch { _interactionEditorIndustries = []; }

            StateHasChanged();
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Deploy dialog
    // ═══════════════════════════════════════════════════════════════════

    private void OpenDeployDialog()
    {
        if (!_canDeploy) return;

        if (EditorState.ActiveTab is { EntityKey: not null } tab
            && DeploymentService.SupportedEntityTypes.Contains(tab.EntityType))
        {
            _deployEntityType = tab.EntityType;
            _deployEntityKey = tab.EntityKey!;
        }
        else if (_interactionEditorOpen && !_interactionEditorIsCreating)
        {
            _deployEntityType = WorldEngineEntityType.Interactions;
            _deployEntityKey = _interactionEditorTag!;
        }

        _deploySourceName = _endpoints
            .FirstOrDefault(ep => ep.Id == EditorState.SelectedEndpointId)?.Name ?? "Unknown";
        _showDeployDialog = true;
    }

    private void CloseDeployDialog()
    {
        _showDeployDialog = false;
        StateHasChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Activity bar
    // ═══════════════════════════════════════════════════════════════════

    private void CloseListPanel() => _listPanelOpen = false;

    private async Task OnActivityBarClick(WorldEngineEntityType entityType)
    {
        // Close region graph if switching away from Regions
        if (_regionGraphOpen && entityType != WorldEngineEntityType.Regions)
        {
            await CloseRegionGraph();
        }

        // Close interaction editor if switching away from Interactions
        if (_interactionEditorOpen && entityType != WorldEngineEntityType.Interactions)
        {
            if (_interactionEditorRef != null) await _interactionEditorRef.Close();
        }

        if (EditorState.ActiveEntityType == entityType)
        {
            _listPanelOpen = !_listPanelOpen;
        }
        else
        {
            EditorState.ActiveEntityType = entityType;
            _listPanelOpen = true;
        }
    }

    private void OnActiveEntityTypeChanged()
    {
        _ = InvokeAsync(async () =>
        {
            _listSearch = "";
            await LoadEntityList(reset: true);

            // Auto-load dialogue editor when Dialogues type is selected
            if (EditorState.ActiveEntityType == WorldEngineEntityType.Dialogues && _dialogueEditor != null)
            {
                _dialogueEditor.SelectEndpoint(EditorState.SelectedEndpointId);
                await _dialogueEditor.LoadListAsync();
            }

            StateHasChanged();
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Entity list loading
    // ═══════════════════════════════════════════════════════════════════

    private async Task OnListSearchInput(string value)
    {
        _listSearch = value;

        // Debounce: cancel any pending search, wait 300ms
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        try
        {
            await Task.Delay(300, token);
            if (!token.IsCancellationRequested)
            {
                await LoadEntityList(reset: true);
                StateHasChanged();
            }
        }
        catch (TaskCanceledException) { /* debounce — expected */ }
    }

    private async Task LoadMoreEntities()
    {
        _listPage++;
        await LoadEntityList(reset: false);
    }

    private async Task LoadEntityList(bool reset)
    {
        if (EditorState.SelectedEndpointId == null || EditorState.ActiveEntityType == null)
        {
            _listItems.Clear();
            _listHasMore = false;
            return;
        }

        if (reset)
        {
            _listPage = 1;
            _listItems.Clear();
        }

        _listLoading = true;
        _listError = null;
        StateHasChanged();

        try
        {
            WorldEngineEntityType type = EditorState.ActiveEntityType.Value;
            string? search = string.IsNullOrWhiteSpace(_listSearch) ? null : _listSearch.Trim();

            List<EntityListItem> fetched = type switch
            {
                WorldEngineEntityType.Items => await LoadItems(search),
                WorldEngineEntityType.ResourceNodes => await LoadResourceNodes(search),
                WorldEngineEntityType.Regions => await LoadRegions(search),
                WorldEngineEntityType.AreaGraph => await LoadAreaGraph(),
                WorldEngineEntityType.Codex => await LoadCodex(search),
                WorldEngineEntityType.Traits => await LoadTraits(search),
                WorldEngineEntityType.Glyphs => await LoadGlyphs(),
                WorldEngineEntityType.Industries => await LoadIndustries(search),
                WorldEngineEntityType.Interactions => await LoadInteractions(search),
                WorldEngineEntityType.Coinhouses => await LoadCoinhouses(search),
                WorldEngineEntityType.Dialogues => await LoadDialogues(search),
                _ => [],
            };

            if (reset)
                _listItems = fetched;
            else
                _listItems.AddRange(fetched);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load entity list for {Type}", EditorState.ActiveEntityType);
            _listError = $"Failed to load: {ex.Message}";
        }
        finally
        {
            _listLoading = false;
        }
    }

    // ── Per-type loaders ────────────────────────────────────────────

    private async Task<List<EntityListItem>> LoadItems(string? search)
    {
        PagedResult<ItemBlueprintDto> result = await ItemApi.GetAllAsync(search, _listPage, ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.ItemTag, i.Name, WorldEngineEntityType.Items)).ToList();
    }

    private async Task<List<EntityListItem>> LoadResourceNodes(string? search)
    {
        PagedResult<ResourceNodeDefinitionDto> result = await ResourceNodeApi.GetAllAsync(search, page: _listPage, pageSize: ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.Tag, i.Name ?? i.Tag, WorldEngineEntityType.ResourceNodes)).ToList();
    }

    private async Task<List<EntityListItem>> LoadRegions(string? search)
    {
        PagedResult<RegionDefinitionDto> result = await RegionApi.GetAllAsync(search, _listPage, ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.Tag, i.Name, WorldEngineEntityType.Regions)).ToList();
    }

    private async Task<List<EntityListItem>> LoadAreaGraph()
    {
        AreaGraphDto? graph = await AreaGraphApi.GetGraphAsync();
        _listHasMore = false;
        if (graph == null) return [];
        List<AreaNodeDto> all = [..graph.Nodes, ..graph.DisconnectedAreas];
        if (!string.IsNullOrWhiteSpace(_listSearch))
        {
            string filter = _listSearch.Trim();
            all = all.Where(n => (n.Name?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                               || n.ResRef.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return all.Select(n => new EntityListItem(n.ResRef, n.Name ?? n.ResRef, WorldEngineEntityType.AreaGraph)).ToList();
    }

    private async Task<List<EntityListItem>> LoadCodex(string? search)
    {
        // Load both lore and quest entries, combine into one list
        Task<PagedResult<LoreDefinitionDto>> loreTask = LoreApi.GetAllAsync(search, page: _listPage, pageSize: ListPageSize);
        Task<PagedResult<QuestDefinitionDto>> questTask = QuestApi.GetAllAsync(search, page: _listPage, pageSize: ListPageSize);
        await Task.WhenAll(loreTask, questTask);

        PagedResult<LoreDefinitionDto> loreResult = await loreTask;
        PagedResult<QuestDefinitionDto> questResult = await questTask;

        _listHasMore = (_listPage * ListPageSize < loreResult.TotalCount) || (_listPage * ListPageSize < questResult.TotalCount);

        List<EntityListItem> items = [];
        items.AddRange(loreResult.Items.Select(i => new EntityListItem(i.LoreId, $"[Lore] {i.Title}", WorldEngineEntityType.Codex)));
        items.AddRange(questResult.Items.Select(i => new EntityListItem(i.QuestId, $"[Quest] {i.Title}", WorldEngineEntityType.Codex)));
        return items;
    }

    private async Task<List<EntityListItem>> LoadTraits(string? search)
    {
        PagedResult<TraitDefinitionDto> result = await TraitApi.GetAllAsync(search, page: _listPage, pageSize: ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.Tag, i.Name, WorldEngineEntityType.Traits)).ToList();
    }

    private async Task<List<EntityListItem>> LoadGlyphs()
    {
        List<GlyphDefinitionDto> all = await GlyphApi.GetAllDefinitionsAsync();
        _listHasMore = false;
        if (!string.IsNullOrWhiteSpace(_listSearch))
        {
            string filter = _listSearch.Trim();
            all = all.Where(g => g.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return all.Select(g => new EntityListItem(g.Id.ToString(), g.Name, WorldEngineEntityType.Glyphs)).ToList();
    }

    private async Task<List<EntityListItem>> LoadIndustries(string? search)
    {
        PagedResult<IndustryDefinitionDto> result = await IndustryApi.GetAllAsync(search, _listPage, ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.Tag, i.Name, WorldEngineEntityType.Industries)).ToList();
    }

    private async Task<List<EntityListItem>> LoadInteractions(string? search)
    {
        PagedResult<InteractionDefinitionDto> result = await InteractionApi.GetAllAsync(search, _listPage, ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.Tag, i.Name, WorldEngineEntityType.Interactions)).ToList();
    }

    private async Task<List<EntityListItem>> LoadCoinhouses(string? search)
    {
        PagedResult<CoinhouseDto> result = await CoinhouseApi.GetAllAsync(search, _listPage, ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.Tag, i.Tag, WorldEngineEntityType.Coinhouses)).ToList();
    }

    private async Task<List<EntityListItem>> LoadDialogues(string? search)
    {
        PagedResult<DialogueTreeDto> result = await DialogueApi.GetAllAsync(search, _listPage, ListPageSize);
        _listHasMore = _listPage * ListPageSize < result.TotalCount;
        return result.Items.Select(i => new EntityListItem(i.DialogueTreeId, i.Title, WorldEngineEntityType.Dialogues)).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  List item click → open tab
    // ═══════════════════════════════════════════════════════════════════

    private async Task OnListItemClick(EntityListItem item)
    {
        if (item.EntityType == WorldEngineEntityType.Regions)
        {
            await OnRegionListItemClick(item);
        }
        else if (item.EntityType == WorldEngineEntityType.Interactions)
        {
            await OpenInteractionEditor(item.Key);
        }
        else if (item.EntityType == WorldEngineEntityType.Codex)
        {
            // Determine sub-type from display name prefix
            CodexEditor.CodexSubType subType = item.DisplayName.StartsWith("[Quest]") ? CodexEditor.CodexSubType.Quest : CodexEditor.CodexSubType.Lore;
            await OpenCodexEditor(item.Key, subType);
        }
        else
        {
            EditorTab tab = EditorState.OpenTab(item.EntityType, item.DisplayName, item.Key);
            // Data will be loaded by OnActiveTabChanged
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Tab data loading
    // ═══════════════════════════════════════════════════════════════════

    private void OnActiveTabChanged()
    {
        _ = InvokeAsync(async () =>
        {
            EditorTab? tab = EditorState.ActiveTab;
            if (tab != null && !_tabData.ContainsKey(tab.Id))
            {
                await LoadTabData(tab);
            }
            StateHasChanged();
        });
    }

    private async Task LoadTabData(EditorTab tab)
    {
        if (tab.EntityKey == null) return;

        _tabDataLoading = true;
        _tabDataError = null;
        StateHasChanged();

        try
        {
            object? data = tab.EntityType switch
            {
                WorldEngineEntityType.Items => await ItemApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.ResourceNodes => await ResourceNodeApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.Regions => await RegionApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.Codex => null, // Codex items open in the full Codex editor
                WorldEngineEntityType.Traits => await TraitApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.Glyphs => await GlyphApi.GetDefinitionAsync(Guid.Parse(tab.EntityKey)),
                WorldEngineEntityType.Industries => await IndustryApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.Interactions => await InteractionApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.Coinhouses => await CoinhouseApi.GetByTagAsync(tab.EntityKey),
                WorldEngineEntityType.AreaGraph => null, // Area graph is a singleton, no per-entity load
                _ => null,
            };

            if (data != null)
                _tabData[tab.Id] = data;
            else
                _tabDataError = "Entity not found.";
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load entity data for tab {TabId}", tab.Id);
            _tabDataError = $"Failed to load: {ex.Message}";
        }
        finally
        {
            _tabDataLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Tabs
    // ═══════════════════════════════════════════════════════════════════

    private void CloseTab(string tabId)
    {
        _tabData.Remove(tabId);
        EditorState.CloseTab(tabId);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private string GetEntityIcon(WorldEngineEntityType type) => EditorCatalog.GetFeature(type).Icon;

    private string GetEntityLabel(WorldEngineEntityType type) => EditorCatalog.GetFeature(type).Label;

    private void StateHasChangedSafe()
    {
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        EditorState.OnEndpointChanged -= OnEditorEndpointChanged;
        EditorState.OnActiveEntityTypeChanged -= OnActiveEntityTypeChanged;
        EditorState.OnActiveTabChanged -= OnActiveTabChanged;
        EditorState.OnTabListChanged -= StateHasChangedSafe;

        // Destroy region graph if open
        if (_regionGraphOpen)
        {
            try { await JS.InvokeVoidAsync("regionGraph.destroy"); } catch { }
            try
            {
                if (_regionBridgeModule != null)
                {
                    await _regionBridgeModule.InvokeVoidAsync("destroy", RegionGlInstanceId);
                    await _regionBridgeModule.DisposeAsync();
                }
            }
            catch { }
        }
        _regionDotNetRef?.Dispose();
    }
}
