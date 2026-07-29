using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Lifecycle
    // ═══════════════════════════════════════════════════════════════════

    private async Task OpenRegionGraph()
    {
        if (_regionGraphOpen) return;

        _regionGraphOpen = true;
        _regionGraphLoading = true;
        _regionGraphError = null;
        _regionGraphLoadPercent = 0;
        _regionGraphLoadPhase = "Fetching data…";
        StateHasChanged();

        try
        {
            // Load graph data and regions in parallel
            Task<AreaGraphDto?> graphTask = AreaGraphApi.GetGraphAsync();
            Task<PagedResult<RegionDefinitionDto>> regionsTask = RegionApi.GetAllAsync(null, 1, 200);

            await Task.WhenAll(graphTask, regionsTask);

            _regionGraphData = await graphTask;
            PagedResult<RegionDefinitionDto> regResult = await regionsTask;
            _rgRegions = regResult.Items;

            if (_regionGraphData == null)
            {
                _regionGraphError = "No area graph data available. Ensure the graph has been generated on the server.";
                _regionGraphLoading = false;
                StateHasChanged();
                return;
            }

            // Load resource definitions for the area editor
            try
            {
                PagedResult<ResourceNodeDefinitionDto> rdResult = await ResourceNodeApi.GetAllAsync(pageSize: 9999);
                _rgAllResourceDefs = rdResult.Items;
            }
            catch { _rgAllResourceDefs = []; }

            RgBuildKnownAreas();
            RgBuildRegionColors();

            _regionGraphLoadPercent = 15;
            _regionGraphLoadPhase = "Computing layout…";
            StateHasChanged();

            // Compute layout server-side
            GraphLayoutResult layoutResult = await RgComputeLayout();

            _regionGraphLoadPercent = 85;
            _regionGraphLoadPhase = "Rendering…";
            _regionGraphNeedsInit = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to open region graph");
            _regionGraphError = $"Failed to load: {ex.Message}";
            _regionGraphLoading = false;
            StateHasChanged();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_regionGraphNeedsInit && _regionGraphData != null)
        {
            _regionGraphNeedsInit = false;
            await InitRegionGraphGl();
        }
    }

    private async Task InitRegionGraphGl()
    {
        try
        {
            // 1. Initialize Golden Layout bridge for this instance
            _regionBridgeModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/golden-layout-bridge.js");
            _regionDotNetRef?.Dispose();
            _regionDotNetRef = DotNetObjectReference.Create(this);

            string layoutConfig = BuildRegionGlLayoutConfig();
            await _regionBridgeModule.InvokeVoidAsync("init", RegionGlInstanceId, "we-region-gl-container", layoutConfig, _regionDotNetRef);

            // 2. Wait a tick for GL to bind the panels, then init Cytoscape
            await Task.Delay(150);

            GraphLayoutResult layoutResult = await RgComputeLayout();

            string nodesJson = JsonSerializer.Serialize(_regionGraphData!.Nodes, RgCamelCase);
            string edgesJson = JsonSerializer.Serialize(_regionGraphData.Edges, RgCamelCase);
            string disconnectedJson = JsonSerializer.Serialize(_regionGraphData.DisconnectedAreas, RgCamelCase);
            string regionsJson = RgBuildRegionsGraphJson();
            string positionsJson = JsonSerializer.Serialize(layoutResult.Positions, RgCamelCase);

            await JS.InvokeVoidAsync("regionGraph.init", "region-cy-gl", nodesJson, edgesJson, disconnectedJson, regionsJson, _regionDotNetRef, positionsJson);

            _regionGraphLoading = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize region graph GL");
            _regionGraphError = $"Failed to initialize: {ex.Message}";
            _regionGraphLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string BuildRegionGlLayoutConfig()
    {
        // Two-panel row: graph (70%) + properties (30%)
        return JsonSerializer.Serialize(new
        {
            root = new
            {
                type = "row",
                content = new object[]
                {
                    new { type = "component", componentType = "regiongraph", title = "Region Graph", size = "70%" },
                    new { type = "component", componentType = "regionprops", title = "Properties", size = "30%" }
                }
            }
        });
    }

    private async Task CloseRegionGraph()
    {
        try { await JS.InvokeVoidAsync("regionGraph.destroy"); } catch { }
        try
        {
            if (_regionBridgeModule != null)
            {
                await _regionBridgeModule.InvokeVoidAsync("destroy", RegionGlInstanceId);
            }
        }
        catch { }

        _regionGraphOpen = false;
        _regionGraphData = null;
        _rgRegions = [];
        _rgKnownAreas = [];
        _rgRegionColors.Clear();
        _rgPanelMode = RgPanelMode.RegionList;
        _rgEditArea = null;
        _rgError = null;
        _rgSuccess = null;
        StateHasChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — GL Bridge Callbacks
    // ═══════════════════════════════════════════════════════════════════

    [JSInvokable]
    public async Task OnPanelResized(string instanceId, string componentType, double width, double height)
    {
        if (instanceId == RegionGlInstanceId && componentType == "regiongraph")
        {
            try { await JS.InvokeVoidAsync("regionGraph.resize"); } catch { }
        }
    }

    [JSInvokable]
    public Task OnPanelRemoved(string instanceId, string componentType)
    {
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnPanelVisibilityChanged(string instanceId, string componentType, bool visible) => Task.CompletedTask;

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Cytoscape JS Callbacks
    // ═══════════════════════════════════════════════════════════════════

    [JSInvokable]
    public Task OnGraphLoadProgress(int percent, string phase)
    {
        _regionGraphLoadPercent = percent;
        _regionGraphLoadPhase = phase;
        if (percent >= 100) _regionGraphLoading = false;
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnGraphNodeSelected(string json)
    {
        try
        {
            var info = JsonSerializer.Deserialize<RgGraphNodeInfo>(json, RgCamelCase);
            if (info != null)
            {
                RgOpenAreaEditor(info.ResRef, info.Region, info.Name);
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "Failed to deserialize graph node selection"); }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnGraphNodeDeselected()
    {
        if (_rgPanelMode == RgPanelMode.AreaEditor)
        {
            _rgPanelMode = RgPanelMode.RegionList;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnRegionParentSelected(string regionTag)
    {
        RegionDefinitionDto? region = _rgRegions.FirstOrDefault(r =>
            string.Equals(r.Tag, regionTag, StringComparison.OrdinalIgnoreCase));
        if (region != null)
        {
            RgSelectRegionForEdit(region);
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnNodeAssignedToRegion(string resRef, string regionTag)
    {
        RegionDefinitionDto? target = _rgRegions.FirstOrDefault(r =>
            string.Equals(r.Tag, regionTag, StringComparison.OrdinalIgnoreCase));
        if (target == null) return;

        foreach (RegionDefinitionDto r in _rgRegions)
            r.Areas.RemoveAll(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));

        if (!target.Areas.Any(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase)))
            target.Areas.Add(new AreaDefinitionDto { ResRef = resRef });

        try
        {
            foreach (RegionDefinitionDto r in _rgRegions) await RegionApi.UpdateAsync(r.Tag, r);
            _rgSuccess = $"Assigned '{resRef}' to '{target.Name}'.";
            await RgRefreshRegions();
            await RgUpdateGraphRegionData();
        }
        catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Failed to assign area"); }
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnNodeUnassigned(string resRef)
    {
        bool changed = false;
        foreach (RegionDefinitionDto r in _rgRegions)
        {
            int removed = r.Areas.RemoveAll(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                try { await RegionApi.UpdateAsync(r.Tag, r); changed = true; }
                catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Failed to unassign area"); }
            }
        }
        if (changed)
        {
            _rgSuccess = $"Unassigned '{resRef}'.";
            await RgRefreshRegions();
            await RgUpdateGraphRegionData();
        }
        StateHasChanged();
    }

    [JSInvokable]
    public Task OnContextMenuAction(string action, string targetId)
    {
        switch (action)
        {
            case "newRegion":
                RgShowCreateRegion();
                break;
            case "deleteRegion":
                RegionDefinitionDto? region = _rgRegions.FirstOrDefault(r =>
                    string.Equals(r.Tag, targetId, StringComparison.OrdinalIgnoreCase));
                if (region != null) { RgSelectRegionForEdit(region); _rgShowDeleteConfirm = true; }
                break;
        }
        StateHasChanged();
        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Graph Interaction
    // ═══════════════════════════════════════════════════════════════════

    private async Task RgSearchInGraph()
    {
        if (string.IsNullOrWhiteSpace(_rgGraphSearchQuery)) return;
        try { await JS.InvokeAsync<bool>("regionGraph.highlightNode", _rgGraphSearchQuery); }
        catch (Exception ex) { Logger.LogError(ex, "Graph search error"); }
    }

    private async Task OnRgGraphSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await RgSearchInGraph();
    }

    private async Task RgHighlightOrphans()
    {
        try { await JS.InvokeVoidAsync("regionGraph.highlightOrphans"); } catch { }
    }

    private async Task RgClearHighlight()
    {
        try { await JS.InvokeVoidAsync("regionGraph.clearHighlight"); } catch { }
    }

    private async Task RgFitView()
    {
        try { await JS.InvokeVoidAsync("regionGraph.fitView"); } catch { }
    }

    private async Task RgHighlightRegion(string regionName)
    {
        try { await JS.InvokeVoidAsync("regionGraph.highlightRegion", regionName); } catch { }
    }

    private async Task RgApplyLayout()
    {
        if (_regionGraphData == null) return;

        _regionGraphLoading = true;
        _regionGraphLoadPercent = 10;
        _regionGraphLoadPhase = "Computing layout…";
        StateHasChanged();

        try
        {
            GraphLayoutResult layoutResult = await RgComputeLayout();
            _regionGraphLoadPercent = 90;
            _regionGraphLoadPhase = "Applying positions…";
            StateHasChanged();

            string positionsJson = JsonSerializer.Serialize(layoutResult.Positions, RgCamelCase);
            await JS.InvokeVoidAsync("regionGraph.applyPositions", positionsJson);

            _regionGraphLoadPercent = 100;
            _regionGraphLoadPhase = "Complete";
            _regionGraphLoading = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply layout");
            _regionGraphError = "Failed to compute layout.";
            _regionGraphLoading = false;
        }
        StateHasChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Data Helpers
    // ═══════════════════════════════════════════════════════════════════

    private async Task RgRefreshRegions()
    {
        try
        {
            PagedResult<RegionDefinitionDto> result = await RegionApi.GetAllAsync(null, 1, 200);
            _rgRegions = result.Items;
        }
        catch (Exception ex) { Logger.LogError(ex, "Failed to refresh regions"); }
    }

    private void RgBuildKnownAreas()
    {
        if (_regionGraphData == null) { _rgKnownAreas = []; return; }
        List<AreaNodeDto> all = [.._regionGraphData.Nodes, .._regionGraphData.DisconnectedAreas];
        all.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        _rgKnownAreas = all;
    }

    private void RgBuildRegionColors()
    {
        _rgRegionColors.Clear();
        string[] palette = ["#e6194b", "#3cb44b", "#4363d8", "#f58231", "#911eb4", "#42d4f4", "#f032e6", "#bfef45", "#fabed4", "#469990", "#dcbeff", "#9A6324"];
        for (int i = 0; i < _rgRegions.Count; i++)
        {
            _rgRegionColors[_rgRegions[i].Tag] = palette[i % palette.Length];
            if (!_rgRegionColors.ContainsKey(_rgRegions[i].Name))
                _rgRegionColors[_rgRegions[i].Name] = palette[i % palette.Length];
        }
    }

    private string RgBuildRegionsGraphJson()
    {
        var data = _rgRegions.Select(r => new
        {
            tag = r.Tag ?? "",
            name = r.Name ?? "",
            areaResRefs = r.Areas.Where(a => !string.IsNullOrWhiteSpace(a.ResRef)).Select(a => a.ResRef).ToList(),
            poiCounts = r.Areas
                .Where(a => !string.IsNullOrWhiteSpace(a.ResRef) && a.PlacesOfInterest?.Count > 0)
                .ToDictionary(a => a.ResRef, a => a.PlacesOfInterest!.Count)
        });
        return JsonSerializer.Serialize(data, RgCamelCase);
    }

    private async Task<GraphLayoutResult> RgComputeLayout()
    {
        List<RegionInfo> regionInfos = _rgRegions.Select(r => new RegionInfo
        {
            Tag = r.Tag,
            Name = r.Name,
            AreaResRefs = r.Areas.Where(a => !string.IsNullOrWhiteSpace(a.ResRef)).Select(a => a.ResRef).ToList()
        }).ToList();

        var progress = new Progress<(int Percent, string Phase)>(p =>
        {
            _regionGraphLoadPercent = 15 + (int)(p.Percent * 0.7);
            _regionGraphLoadPhase = p.Phase;
            InvokeAsync(StateHasChanged);
        });

        return await LayoutService.ComputeLayoutAsync(_regionGraphData!, regionInfos, _rgSelectedLayout, new Dictionary<string, object>(), progress);
    }

    private async Task RgUpdateGraphRegionData()
    {
        if (_regionGraphData == null) return;
        try
        {
            Dictionary<string, string> assignments = new(StringComparer.OrdinalIgnoreCase);
            foreach (RegionDefinitionDto region in _rgRegions)
                foreach (AreaDefinitionDto area in region.Areas)
                    if (!string.IsNullOrWhiteSpace(area.ResRef))
                        assignments[area.ResRef] = region.Tag;

            foreach (AreaNodeDto node in _regionGraphData.Nodes.Concat(_regionGraphData.DisconnectedAreas))
                node.Region = assignments.TryGetValue(node.ResRef, out string? tag) ? tag : null;

            RgBuildRegionColors();
            string regionsJson = RgBuildRegionsGraphJson();
            await JS.InvokeVoidAsync("regionGraph.updateRegionData", regionsJson);
            StateHasChanged();
        }
        catch (Exception ex) { Logger.LogError(ex, "Failed to update graph region data"); }
    }

    private string? RgGetAreaName(string? resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef) || _rgKnownAreas.Count == 0) return null;
        return _rgKnownAreas.FirstOrDefault(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase))?.Name;
    }

    private IEnumerable<RegionDefinitionDto> RgFilteredRegions()
    {
        if (string.IsNullOrWhiteSpace(_rgRegionListFilter)) return _rgRegions;
        string f = _rgRegionListFilter.Trim();
        return _rgRegions.Where(r => r.Name.Contains(f, StringComparison.OrdinalIgnoreCase) || r.Tag.Contains(f, StringComparison.OrdinalIgnoreCase));
    }

    private bool RgIsRegionSelected(RegionDefinitionDto region) =>
        _rgPanelMode == RgPanelMode.RegionEditor && !_rgIsCreating &&
        string.Equals(_rgEditRegion.Tag, region.Tag, StringComparison.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Panel Mode Management
    // ═══════════════════════════════════════════════════════════════════

    private void RgShowCreateRegion()
    {
        _rgIsCreating = true;
        _rgEditRegion = new RegionDefinitionDto();
        _rgShowDeleteConfirm = false;
        _rgShowAddAreaInput = false;
        _rgPanelMode = RgPanelMode.RegionEditor;
    }

    private void RgSelectRegionForEdit(RegionDefinitionDto region)
    {
        _rgIsCreating = false;
        _rgEditRegion = RgCloneRegion(region);
        _rgShowDeleteConfirm = false;
        _rgShowAddAreaInput = false;
        _rgPanelMode = RgPanelMode.RegionEditor;
    }

    private void RgCloseEditor()
    {
        _rgPanelMode = RgPanelMode.RegionList;
        _rgEditArea = null;
        _rgShowDeleteConfirm = false;
        _rgShowAddAreaInput = false;
    }

    private void RgOpenAreaEditor(string resRef, string? regionTag, string? areaName)
    {
        _rgEditAreaRegionTag = regionTag ?? "";
        _rgEditAreaOriginalRegionTag = _rgEditAreaRegionTag;
        _rgEditAreaName = areaName;

        AreaDefinitionDto? found = null;
        if (!string.IsNullOrEmpty(regionTag))
        {
            RegionDefinitionDto? region = _rgRegions.FirstOrDefault(r => string.Equals(r.Tag, regionTag, StringComparison.OrdinalIgnoreCase));
            found = region?.Areas.FirstOrDefault(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
        }
        if (found == null)
        {
            foreach (RegionDefinitionDto r in _rgRegions)
            {
                found = r.Areas.FirstOrDefault(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
                if (found != null) { _rgEditAreaRegionTag = r.Tag; _rgEditAreaOriginalRegionTag = r.Tag; break; }
            }
        }

        _rgEditArea = found != null ? RgCloneArea(found) : new AreaDefinitionDto { ResRef = resRef };
        _rgPanelMode = RgPanelMode.AreaEditor;
        StateHasChanged();
    }

    private void RgEditAreaInRegion(int areaIndex)
    {
        AreaDefinitionDto area = _rgEditRegion.Areas[areaIndex];
        _rgEditAreaRegionTag = _rgEditRegion.Tag;
        _rgEditAreaOriginalRegionTag = _rgEditRegion.Tag;
        _rgEditAreaName = RgGetAreaName(area.ResRef);
        _rgEditArea = RgCloneArea(area);
        _rgPanelMode = RgPanelMode.AreaEditor;
    }

    private void RgConfirmAddArea()
    {
        if (string.IsNullOrWhiteSpace(_rgNewAreaResRef)) return;
        _rgEditRegion.Areas.Add(new AreaDefinitionDto { ResRef = _rgNewAreaResRef.Trim() });
        _rgNewAreaResRef = "";
        _rgShowAddAreaInput = false;
    }

    private void RgOpenAddArea()
    {
        _rgShowAddAreaInput = true;
        _rgNewAreaResRef = "";
    }

    private void RgOnAreaRegionChanged(ChangeEventArgs e)
    {
        _rgEditAreaRegionTag = e.Value?.ToString() ?? "";
    }

    private void RgAddPoi()
    {
        if (_rgEditArea == null) return;
        _rgEditArea.PlacesOfInterest ??= new List<PlaceOfInterestDto>();
        _rgEditArea.PlacesOfInterest.Add(new PlaceOfInterestDto());
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Save / Delete
    // ═══════════════════════════════════════════════════════════════════

    private async Task RgSaveCurrentEditor()
    {
        if (_rgPanelMode == RgPanelMode.RegionEditor)
            await RgSaveRegion();
        else if (_rgPanelMode == RgPanelMode.AreaEditor)
            await RgSaveArea();
    }

    private async Task RgSaveRegion()
    {
        _rgIsSaving = true;
        _rgError = null;
        try
        {
            if (_rgIsCreating)
                await RegionApi.CreateAsync(_rgEditRegion);
            else
                await RegionApi.UpdateAsync(_rgEditRegion.Tag, _rgEditRegion);

            _rgSuccess = _rgIsCreating ? $"Region '{_rgEditRegion.Name}' created." : $"Region '{_rgEditRegion.Name}' updated.";
            await RgRefreshRegions();
            RgBuildRegionColors();
            await RgUpdateGraphRegionData();
            _rgPanelMode = RgPanelMode.RegionList;
        }
        catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Failed to save region"); }
        finally { _rgIsSaving = false; StateHasChanged(); }
    }

    private async Task RgSaveArea()
    {
        if (_rgEditArea == null) return;
        _rgIsSaving = true;
        _rgError = null;
        try
        {
            string resRef = _rgEditArea.ResRef;
            string? newRegionTag = _rgEditAreaRegionTag;
            string? oldRegionTag = _rgEditAreaOriginalRegionTag;

            if (!string.Equals(oldRegionTag, newRegionTag, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(oldRegionTag))
                {
                    RegionDefinitionDto? oldRegion = _rgRegions.FirstOrDefault(r => string.Equals(r.Tag, oldRegionTag, StringComparison.OrdinalIgnoreCase));
                    if (oldRegion != null)
                    {
                        oldRegion.Areas.RemoveAll(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
                        await RegionApi.UpdateAsync(oldRegion.Tag, oldRegion);
                    }
                }
                if (!string.IsNullOrEmpty(newRegionTag))
                {
                    RegionDefinitionDto? newRegion = _rgRegions.FirstOrDefault(r => string.Equals(r.Tag, newRegionTag, StringComparison.OrdinalIgnoreCase));
                    if (newRegion != null)
                    {
                        newRegion.Areas.RemoveAll(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
                        newRegion.Areas.Add(RgCloneArea(_rgEditArea));
                        await RegionApi.UpdateAsync(newRegion.Tag, newRegion);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(newRegionTag))
            {
                RegionDefinitionDto? targetRegion = _rgRegions.FirstOrDefault(r => string.Equals(r.Tag, newRegionTag, StringComparison.OrdinalIgnoreCase));
                if (targetRegion != null)
                {
                    int idx = targetRegion.Areas.FindIndex(a => string.Equals(a.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) targetRegion.Areas[idx] = RgCloneArea(_rgEditArea);
                    else targetRegion.Areas.Add(RgCloneArea(_rgEditArea));
                    await RegionApi.UpdateAsync(targetRegion.Tag, targetRegion);
                }
            }

            _rgSuccess = $"Area '{resRef}' saved.";
            await RgRefreshRegions();
            RgBuildRegionColors();
            await RgUpdateGraphRegionData();
            _rgPanelMode = RgPanelMode.RegionList;
        }
        catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Failed to save area"); }
        finally { _rgIsSaving = false; StateHasChanged(); }
    }

    private async Task RgDeleteRegion()
    {
        _rgIsSaving = true;
        _rgError = null;
        try
        {
            await RegionApi.DeleteAsync(_rgEditRegion.Tag);
            _rgSuccess = $"Deleted region '{_rgEditRegion.Name}'.";
            _rgShowDeleteConfirm = false;
            _rgPanelMode = RgPanelMode.RegionList;
            await RgRefreshRegions();
            RgBuildRegionColors();
            await RgUpdateGraphRegionData();
        }
        catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Failed to delete region"); }
        finally { _rgIsSaving = false; StateHasChanged(); }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Export / Import
    // ═══════════════════════════════════════════════════════════════════

    private async Task RgExportRegions()
    {
        _rgIsExporting = true;
        _rgError = null;
        StateHasChanged();
        try
        {
            string json = await RegionApi.ExportJsonAsync(_listSearch);
            string fileName = $"regions-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            string base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("adminPanelDownloadFile", fileName, base64);
            _rgSuccess = "Export downloaded.";
        }
        catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Region export failed"); }
        finally { _rgIsExporting = false; StateHasChanged(); }
    }

    private async Task RgOnImportFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            foreach (IBrowserFile file in e.GetMultipleFiles(100))
            {
                if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    await RgProcessZipRegions(file);
                else
                    await RgProcessJsonFileRegions(file);
            }
            _rgImportJson = JsonSerializer.Serialize(_rgImportedRegions);
            _rgImportResult = null;
            _rgSuccess = $"Loaded {_rgImportedRegions.Count} region(s). Click Import to apply.";
            StateHasChanged();
        }
        catch (Exception ex) { _rgError = $"Failed to read file(s): {ex.Message}"; }
    }

    private async Task RgProcessJsonFileRegions(IBrowserFile file)
    {
        using StreamReader reader = new StreamReader(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
        RgParseJsonRegions(await reader.ReadToEndAsync());
    }

    private async Task RgProcessZipRegions(IBrowserFile file)
    {
        using MemoryStream ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024).CopyToAsync(ms);
        ms.Position = 0;
        using ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            using Stream entryStream = entry.Open();
            using StreamReader reader = new StreamReader(entryStream);
            RgParseJsonRegions(await reader.ReadToEndAsync());
        }
    }

    private void RgParseJsonRegions(string content)
    {
        using JsonDocument doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in doc.RootElement.EnumerateArray())
                _rgImportedRegions.Add(item.Clone());
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            _rgImportedRegions.Add(doc.RootElement.Clone());
        }
    }

    private async Task RgRunImport()
    {
        if (string.IsNullOrWhiteSpace(_rgImportJson)) return;
        _rgIsSaving = true;
        _rgError = null;
        try
        {
            _rgImportResult = await RegionApi.ImportJsonAsync(_rgImportJson);
            if (_rgImportResult is { Failed: 0 })
                _rgSuccess = $"Successfully imported {_rgImportResult.Succeeded} regions.";
            await RgRefreshRegions();
            RgBuildRegionColors();
            if (_regionGraphData != null) await RgUpdateGraphRegionData();
        }
        catch (Exception ex) { _rgError = ex.Message; Logger.LogError(ex, "Region import failed"); }
        finally { _rgIsSaving = false; StateHasChanged(); }
    }

    private void RgCloseImportModal()
    {
        _rgShowImportModal = false;
        _rgImportJson = "";
        _rgImportResult = null;
        _rgImportedRegions.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Resource Def Picker
    // ═══════════════════════════════════════════════════════════════════

    private List<ResourceNodeDefinitionDto> RgGetFilteredResourceDefs()
    {
        List<string> selected = _rgEditArea?.DefinitionTags ?? [];
        string query = _rgDefTagSearch?.Trim() ?? "";
        return _rgAllResourceDefs
            .Where(d => !selected.Contains(d.Tag))
            .Where(d => string.IsNullOrEmpty(query)
                        || d.Tag.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (d.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (d.Type?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(d => d.Tag)
            .ToList();
    }

    private void RgAddDefinitionTag(string tag)
    {
        if (_rgEditArea == null) return;
        if (!_rgEditArea.DefinitionTags.Contains(tag))
            _rgEditArea.DefinitionTags.Add(tag);
        _rgDefTagSearch = "";
        _rgDefTagDropdownOpen = false;
    }

    private async Task RgOnDefTagBlur()
    {
        await Task.Delay(200);
        _rgDefTagDropdownOpen = false;
        StateHasChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — List Item Click → Graph Highlight
    // ═══════════════════════════════════════════════════════════════════

    private async Task OnRegionListItemClick(EntityListItem item)
    {
        if (_regionGraphOpen)
        {
            // Highlight in graph and open properties
            try { await JS.InvokeVoidAsync("regionGraph.highlightRegion", item.DisplayName); } catch { }
            RegionDefinitionDto? region = _rgRegions.FirstOrDefault(r => string.Equals(r.Tag, item.Key, StringComparison.OrdinalIgnoreCase));
            if (region != null)
            {
                RgSelectRegionForEdit(region);
                StateHasChanged();
            }
        }
        else
        {
            // Normal behavior: open a tab
            EditorState.OpenTab(item.EntityType, item.DisplayName, item.Key);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Region Graph — Clone Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static RegionDefinitionDto RgCloneRegion(RegionDefinitionDto source) => new()
    {
        Tag = source.Tag,
        Name = source.Name,
        DefaultChaos = source.DefaultChaos != null ? new ChaosStateDto
        {
            Danger = source.DefaultChaos.Danger,
            Corruption = source.DefaultChaos.Corruption,
            Density = source.DefaultChaos.Density,
            Mutation = source.DefaultChaos.Mutation
        } : null,
        Areas = source.Areas.Select(RgCloneArea).ToList()
    };

    private static AreaDefinitionDto RgCloneArea(AreaDefinitionDto a) => new()
    {
        ResRef = a.ResRef,
        DefinitionTags = a.DefinitionTags.ToList(),
        LinkedSettlement = a.LinkedSettlement,
        Environment = new EnvironmentDataDto
        {
            Climate = a.Environment.Climate,
            SoilQuality = a.Environment.SoilQuality,
            MineralQualityRange = new QualityRangeDto
            {
                Min = a.Environment.MineralQualityRange.Min,
                Max = a.Environment.MineralQualityRange.Max
            },
            Chaos = a.Environment.Chaos != null ? new ChaosStateDto
            {
                Danger = a.Environment.Chaos.Danger,
                Corruption = a.Environment.Chaos.Corruption,
                Density = a.Environment.Chaos.Density,
                Mutation = a.Environment.Chaos.Mutation
            } : null
        },
        PlacesOfInterest = a.PlacesOfInterest?.Select(p => new PlaceOfInterestDto
        {
            ResRef = p.ResRef, Tag = p.Tag, Name = p.Name, Type = p.Type, Description = p.Description
        }).ToList()
    };

    private sealed class RgGraphNodeInfo
    {
        public string ResRef { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Region { get; set; }
    }
}
