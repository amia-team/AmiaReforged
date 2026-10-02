namespace AmiaReforged.Shared.Dialogue;

public record DialoguePlaybackResult(bool Success, string? Error = null);

/// <summary>Conversation navigation shared by the server and the authoring preview.
/// Callbacks supply conditions and node effects; navigation itself has no game dependencies.</summary>
public sealed class DialoguePlayback(DialogueTreeDto tree)
{
    public string? CurrentNodeId { get; private set; }
    public DialogueNodeDto? CurrentNode => tree.Nodes.FirstOrDefault(n => n.Id == CurrentNodeId);
    public bool IsBusy { get; private set; }
    public bool IsTerminal => CurrentNode?.Type == "End";

    public async Task<DialoguePlaybackResult> StartAsync(
        Func<IReadOnlyList<DialogueConditionDto>, Task<bool>> evaluate,
        Func<DialogueNodeDto, Task<bool>> enter)
    {
        if (IsBusy || CurrentNodeId != null) return new(false, "Conversation has already started");
        IsBusy = true;
        try
        {
            DialogueNodeDto? root = null;
            foreach (DialogueNodeDto candidate in tree.Nodes.Where(n => n.Type == "Root" && n.Conditions.Count > 0).OrderBy(n => n.SortOrder).ThenBy(n => n.Id))
                if (await evaluate(candidate.Conditions)) { root = candidate; break; }
            root ??= tree.Nodes.FirstOrDefault(n => n.Id == tree.RootNodeId && n.Type == "Root" && n.Conditions.Count == 0);
            return root is null ? new(false, "No greeting is available") : await EnterAsync(root, evaluate, enter);
        }
        finally { IsBusy = false; }
    }

    public async Task<List<DialogueChoiceDto>> GetVisibleChoicesAsync(Func<IReadOnlyList<DialogueConditionDto>, Task<bool>> evaluate)
    {
        DialogueNodeDto? node = CurrentNode;
        if (node is null || node.Type is "End" or "Action") return [];
        List<DialogueChoiceDto> visible = [];
        foreach (DialogueChoiceDto choice in node.Choices.OrderBy(c => c.SortOrder))
        {
            DialogueNodeDto? target = tree.Nodes.FirstOrDefault(n => n.Id == choice.TargetNodeId);
            if (target != null && await evaluate(choice.Conditions) && await evaluate(target.Conditions)) visible.Add(choice);
        }
        return visible;
    }

    public async Task<DialoguePlaybackResult> ChooseAsync(string expectedNodeId, string choiceId,
        Func<IReadOnlyList<DialogueConditionDto>, Task<bool>> evaluate,
        Func<DialogueNodeDto, Task<bool>> enter)
    {
        if (IsBusy) return new(false, "Conversation is advancing");
        IsBusy = true;
        try
        {
            DialogueNodeDto? node = CurrentNode;
            if (node is null || node.Id != expectedNodeId || IsTerminal) return new(false, "This reply is no longer available");
            DialogueChoiceDto? choice = node.Choices.FirstOrDefault(c => c.Id == choiceId);
            DialogueNodeDto? target = tree.Nodes.FirstOrDefault(n => n.Id == choice?.TargetNodeId);
            if (choice is null || target is null || !await evaluate(choice.Conditions) || !await evaluate(target.Conditions)) return new(false, "This reply is no longer available");
            // A close/cancel can arrive during condition evaluation. The caller's entry
            // callback must also check its session is still active before executing effects.
            return await EnterAsync(target, evaluate, enter);
        }
        finally { IsBusy = false; }
    }

    private async Task<DialoguePlaybackResult> EnterAsync(DialogueNodeDto node,
        Func<IReadOnlyList<DialogueConditionDto>, Task<bool>> evaluate,
        Func<DialogueNodeDto, Task<bool>> enter)
    {
        HashSet<string> automaticNodes = [];
        while (true)
        {
            if (!automaticNodes.Add(node.Id)) return new(false, "Automatic dialogue cycle detected");
            CurrentNodeId = node.Id;
            if (!await enter(node)) return new(false, "Dialogue action failed");
            if (node.Type != "Action") return new(true);
            if (node.Choices.Count != 1) return new(false, "Action node needs one continuation");
            DialogueChoiceDto choice = node.Choices[0];
            DialogueNodeDto? target = tree.Nodes.FirstOrDefault(n => n.Id == choice.TargetNodeId);
            if (target is null || !await evaluate(choice.Conditions) || !await evaluate(target.Conditions)) return new(false, "Action continuation is unavailable");
            node = target;
        }
    }
}
