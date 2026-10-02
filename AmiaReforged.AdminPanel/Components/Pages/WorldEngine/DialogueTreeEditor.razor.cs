using System.Text.Json;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using AmiaReforged.Shared.Dialogue;

namespace AmiaReforged.AdminPanel.Components.Pages.WorldEngine;

public partial class DialogueTreeEditor
{
    private List<DialogueTreeDto> _dialogueList = [];
    private DialogueTreeDto? _editItem;
    private DialogueNodeDto? _selectedNode;
    private DialogueNodeDto? _pendingDelete;
    private bool _isCreating;
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;
    private Guid? _endpointId;
    private string _endpointName = "No server selected";
    private DialogueRuntimeStatusDto? _runtimeStatus;
    private DialoguePlayback? _preview;
    private List<DialogueChoiceDto> _previewChoices = [];
    private List<DialogueConditionDto> _previewConditions = [];
    private readonly Dictionary<string, bool> _previewConditionResults = [];
    private readonly List<string> _previewActions = [];
    private string? _previewError;

    public void SelectEndpoint(Guid? endpointId)
    {
        Api.SelectEndpoint(endpointId);
        if (_endpointId == endpointId) return;
        _endpointId = endpointId;
        _endpointName = "Selected server";
        BackToList();
        _dialogueList = [];
    }

    public async Task LoadListAsync()
    {
        BackToList();
        _isLoading = true;
        Guid? endpoint = _endpointId;
        StateHasChanged();
        try
        {
            string endpointName = endpoint is { } id ? (await Endpoints.GetEndpointAsync(id))?.Name ?? "Selected server" : "No server selected";
            List<DialogueTreeDto> items = [];
            for (int page = 1; ; page++)
            {
                PagedResult<DialogueTreeDto> result = await Api.GetAllAsync(page: page, pageSize: 200);
                if (_endpointId != endpoint) return;
                items.AddRange(result.Items);
                if (result.Items.Count == 0 || items.Count >= result.TotalCount) break;
            }
            _dialogueList = items;
            _endpointName = endpointName;
        }
        catch (Exception ex) { if (_endpointId == endpoint) _errorMessage = $"Could not load dialogues: {ex.Message}"; }
        finally { _isLoading = false; StateHasChanged(); }
    }

    private void CreateNew()
    {
        DialogueNodeDto greeting = DialogueAuthoring.CreateNode("Root");
        greeting.Name = "Default greeting";
        greeting.Text = "Hello.";
        _editItem = new() { Title = "New Dialogue", RootNodeId = greeting.Id, Nodes = [greeting] };
        _selectedNode = greeting;
        _isCreating = true;
        _runtimeStatus = null;
        _errorMessage = null;
        _preview = null;
    }

    private async Task OpenEdit(DialogueTreeDto item)
    {
        Guid? endpoint = _endpointId;
        try
        {
            DialogueTreeDto? full = await Api.GetByIdAsync(item.DialogueTreeId);
            if (_endpointId != endpoint) return;
            if (full == null) { _errorMessage = "Could not load dialogue."; return; }
            DialogueDefinitionValidator.Normalize(full);
            _editItem = full;
            _selectedNode = full.Nodes.FirstOrDefault(n => n.Id == full.RootNodeId) ?? full.Nodes.FirstOrDefault();
            _isCreating = false;
            _errorMessage = null;
            _preview = null;
            _runtimeStatus = null;
            await RefreshRuntimeStatusAsync();
        }
        catch (Exception ex) { if (_endpointId == endpoint) _errorMessage = $"Could not load dialogue: {ex.Message}"; }
    }

    private async Task DeleteAsync(DialogueTreeDto item)
    {
        Guid? endpoint = _endpointId;
        try
        {
            await Api.DeleteAsync(item.DialogueTreeId);
            if (_endpointId != endpoint) return;
            _dialogueList.Remove(item);
            if (_editItem?.DialogueTreeId == item.DialogueTreeId) BackToList();
        }
        catch (Exception ex) { if (_endpointId == endpoint) _errorMessage = $"Delete failed: {ex.Message}"; }
    }

    private void BackToList()
    {
        _editItem = null;
        _selectedNode = null;
        _pendingDelete = null;
        _preview = null;
        _runtimeStatus = null;
        _isCreating = false;
        _errorMessage = null;
    }

    private async Task SaveAsync()
    {
        if (_editItem is null || _isSaving) return;
        DialogueDefinitionValidator.Normalize(_editItem);
        List<string> errors = DialogueDefinitionValidator.Validate(_editItem);
        if (errors.Count > 0) { _errorMessage = string.Join("\n", errors); return; }
        DialogueTreeDto savingItem = _editItem;
        Guid? endpoint = _endpointId;
        string? selectedId = _selectedNode?.Id;
        _isSaving = true;
        _errorMessage = null;
        _runtimeStatus = null;
        try
        {
            DialogueTreeDto? saved = _isCreating ? await Api.CreateAsync(savingItem) : await Api.UpdateAsync(savingItem.DialogueTreeId, savingItem);
            if (_endpointId != endpoint || !ReferenceEquals(_editItem, savingItem)) return;
            if (saved == null) { _errorMessage = "The server did not return a saved dialogue."; return; }
            _editItem = saved;
            _selectedNode = saved.Nodes.FirstOrDefault(n => n.Id == selectedId);
            int index = _dialogueList.FindIndex(t => t.DialogueTreeId == saved.DialogueTreeId);
            if (index >= 0) _dialogueList[index] = saved; else _dialogueList.Add(saved);
            _isCreating = false;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (_endpointId != endpoint || !ReferenceEquals(_editItem, saved)) break;
                await RefreshRuntimeStatusAsync();
                StateHasChanged();
                if (_runtimeStatus?.State != "Pending") break;
                await Task.Delay(500);
            }
        }
        catch (Exception ex) { if (_endpointId == endpoint) _errorMessage = $"Save failed: {ex.Message}"; }
        finally { _isSaving = false; }
    }

    private async Task RefreshRuntimeStatusAsync()
    {
        if (_editItem is null || _isCreating) return;
        DialogueTreeDto item = _editItem;
        Guid? endpoint = _endpointId;
        try
        {
            DialogueRuntimeStatusDto? status = await Api.GetRuntimeStatusAsync(item.DialogueTreeId);
            if (_endpointId == endpoint && ReferenceEquals(_editItem, item)) _runtimeStatus = status;
        }
        catch (Exception ex)
        {
            if (_endpointId == endpoint && ReferenceEquals(_editItem, item)) _errorMessage = $"Runtime status could not be checked: {ex.Message}";
        }
    }

    private void SelectNode(DialogueNodeDto node) { _selectedNode = node; _pendingDelete = null; }
    private static string NodeLabel(DialogueNodeDto node) => DialogueFlowOutline.Label(node);
    private int IncomingReplyCount(string id) => _editItem!.Nodes.Sum(n => n.Choices.Count(c => c.TargetNodeId == id));
    private List<DialogueNodeDto> UnlinkedNodes
    {
        get
        {
            if (_editItem is null) return [];
            HashSet<string> reachable = [];
            Stack<string> pending = new(_editItem.Nodes.Where(n => n.Type == "Root").Select(n => n.Id));
            while (pending.TryPop(out string? id))
            {
                if (!reachable.Add(id)) continue;
                if (_editItem.Nodes.FirstOrDefault(n => n.Id == id) is { } node)
                    foreach (DialogueChoiceDto choice in node.Choices) pending.Push(choice.TargetNodeId);
            }
            return _editItem.Nodes.Where(n => !reachable.Contains(n.Id)).ToList();
        }
    }

    private void AddRootNode()
    {
        DialogueNodeDto node = DialogueAuthoring.CreateNode("Root");
        node.SortOrder = _editItem!.Nodes.Where(n => n.Type == "Root").Select(n => n.SortOrder).DefaultIfEmpty(-1).Max() + 1;
        _editItem.Nodes.Add(node);
        _editItem.RootNodeId ??= node.Id;
        SelectNode(node);
    }

    private void SetAsRoot(DialogueNodeDto node) { node.Type = "Root"; _editItem!.RootNodeId = node.Id; }
    private void ChangeNodeType(Microsoft.AspNetCore.Components.ChangeEventArgs evt)
    {
        if (_selectedNode is null) return;
        _selectedNode.Type = evt.Value?.ToString() ?? "NpcText";
        if (_selectedNode.Type != "Root" && _editItem!.RootNodeId == _selectedNode.Id)
            _editItem.RootNodeId = _editItem.Nodes.FirstOrDefault(n => n.Type == "Root")?.Id;
        if (_selectedNode.Type == "Root" && _editItem!.RootNodeId is null) _editItem.RootNodeId = _selectedNode.Id;
    }

    private void AddReply(DialogueNodeDto node) => DialogueAuthoring.AddReply(node);
    private void AddDestination(DialogueNodeDto node, bool continuation, bool ending)
    {
        DialogueChoiceDto choice = DialogueAuthoring.AddReply(node, continuation && !ending);
        if (ending && node.Type != "Action") choice.ResponseText = "Goodbye";
        DialogueNodeDto destination = DialogueAuthoring.CreateNode(ending ? "End" : "NpcText");
        destination.Name = ending ? "End conversation" : "";
        _editItem!.Nodes.Add(destination);
        choice.TargetNodeId = destination.Id;
        if (!ending) SelectNode(destination);
    }

    private void CreateReplyDestination(DialogueChoiceDto choice)
    {
        DialogueNodeDto destination = DialogueAuthoring.CreateNode("NpcText");
        _editItem!.Nodes.Add(destination);
        choice.TargetNodeId = destination.Id;
        SelectNode(destination);
    }

    private void MoveReply(DialogueNodeDto node, DialogueChoiceDto choice, int offset) => DialogueAuthoring.MoveReply(node, choice, offset);
    private void RemoveNode(DialogueNodeDto node)
    {
        DialogueAuthoring.RemoveNode(_editItem!, node);
        _pendingDelete = null;
        _selectedNode = _editItem!.Nodes.FirstOrDefault(n => n.Id == _editItem.RootNodeId);
        _preview = null;
    }
    private static void AddAction(DialogueNodeDto node) => node.Actions.Add(new() { ActionType = "StartQuest", ExecutionOrder = node.Actions.Count });
    private static void AddCondition(DialogueChoiceDto choice) => choice.Conditions.Add(new() { Type = "QuestState", Parameters = new() { ["requiredState"] = "NotStarted" } });
    private static void AddNodeCondition(DialogueNodeDto node) => node.Conditions.Add(new() { Type = "QuestState", Parameters = new() { ["requiredState"] = "NotStarted" } });

    private static string ConditionKey(DialogueConditionDto condition) => condition.Type + ":" + JsonSerializer.Serialize(condition.Parameters.OrderBy(p => p.Key));
    private Task<bool> EvaluatePreviewAsync(IReadOnlyList<DialogueConditionDto> conditions) => Task.FromResult(conditions.All(c => _previewConditionResults.GetValueOrDefault(ConditionKey(c), true) != c.Negate));
    private Task<bool> EnterPreviewAsync(DialogueNodeDto node)
    {
        _previewActions.AddRange(node.Actions.OrderBy(a => a.ExecutionOrder).Select(a => $"{a.ActionType} ({string.Join(", ", a.Parameters.Select(p => $"{p.Key}={p.Value}"))})"));
        return Task.FromResult(true);
    }

    private async Task StartPreviewAsync()
    {
        if (_editItem is null) return;
        DialogueTreeDto snapshot = JsonSerializer.Deserialize<DialogueTreeDto>(JsonSerializer.Serialize(_editItem))!;
        DialogueDefinitionValidator.Normalize(snapshot);
        List<string> errors = DialogueDefinitionValidator.Validate(snapshot);
        if (errors.Count > 0) { _errorMessage = string.Join("\n", errors); return; }
        _errorMessage = null;
        _preview = new(snapshot);
        _previewActions.Clear();
        _previewConditions = snapshot.Nodes.SelectMany(n => n.Conditions.Concat(n.Choices.SelectMany(c => c.Conditions))).ToList();
        foreach (DialogueConditionDto condition in _previewConditions) _previewConditionResults.TryAdd(ConditionKey(condition), true);
        DialoguePlaybackResult result = await _preview.StartAsync(EvaluatePreviewAsync, EnterPreviewAsync);
        _previewError = result.Error;
        _previewChoices = result.Success ? await _preview.GetVisibleChoicesAsync(EvaluatePreviewAsync) : [];
    }

    private async Task ChoosePreviewAsync(DialogueChoiceDto choice)
    {
        if (_preview?.CurrentNodeId is not { } nodeId) return;
        DialoguePlaybackResult result = await _preview.ChooseAsync(nodeId, choice.Id, EvaluatePreviewAsync, EnterPreviewAsync);
        _previewError = result.Error;
        _previewChoices = result.Success ? await _preview.GetVisibleChoicesAsync(EvaluatePreviewAsync) : [];
        if (_preview.IsTerminal && string.IsNullOrWhiteSpace(_preview.CurrentNode?.Text)) _preview = null;
    }
}
