namespace AmiaReforged.Shared.Dialogue;

/// <summary>The same definition contract is used by authoring, API writes, and playback.</summary>
public static class DialogueDefinitionValidator
{
    public static readonly string[] NodeTypes = ["Root", "NpcText", "Action", "End"];
    public static readonly string[] ActionTypes = ["StartQuest", "CompleteQuest", "GiveItem", "TakeItem", "ChangeReputation", "GrantKnowledge", "SetLocalVariable", "GiveGold", "TakeGold", "OpenShop", "SetQuestStage"];
    public static readonly string[] ConditionTypes = ["HasItem", "QuestState", "ReputationAbove", "ReputationBelow", "HasKnowledge", "LocalVariable", "Custom"];

    // Old definitions have no reply IDs. Assign IDs on read/save without changing node IDs
    // (node IDs are also quest objective references).
    public static void Normalize(DialogueTreeDto tree)
    {
        tree.Nodes ??= [];
        tree.RootNodeId = CanonicalId(tree.RootNodeId);
        foreach (DialogueNodeDto node in tree.Nodes.Where(n => n != null))
        {
            node.Id = CanonicalId(node.Id) ?? "";
            node.ParentNodeId = CanonicalId(node.ParentNodeId);
            node.Type = NodeTypes.FirstOrDefault(t => t.Equals(node.Type, StringComparison.OrdinalIgnoreCase)) ?? node.Type;
            node.Conditions ??= [];
            node.Choices ??= [];
            node.Actions ??= [];
            foreach (DialogueChoiceDto choice in node.Choices.Where(c => c != null))
            {
                if (string.IsNullOrEmpty(choice.Id) && string.IsNullOrWhiteSpace(choice.ResponseText)) choice.IsContinue = true;
                choice.Id = string.IsNullOrEmpty(choice.Id) ? Guid.NewGuid().ToString() : CanonicalId(choice.Id)!;
                choice.TargetNodeId = CanonicalId(choice.TargetNodeId) ?? "";
                choice.Conditions ??= [];
            }
            foreach (DialogueActionDto action in node.Actions.Where(a => a != null))
                action.ActionType = ActionTypes.FirstOrDefault(t => t.Equals(action.ActionType, StringComparison.OrdinalIgnoreCase)) ?? action.ActionType;
            foreach (DialogueConditionDto condition in node.Conditions.Concat(node.Choices.Where(c => c != null).SelectMany(c => c.Conditions)).Where(c => c != null))
                condition.Type = ConditionTypes.FirstOrDefault(t => t.Equals(condition.Type, StringComparison.OrdinalIgnoreCase)) ?? condition.Type;
        }
    }

    private static string? CanonicalId(string? id) => Guid.TryParse(id, out Guid value) ? value.ToString() : id;

    public static List<string> Validate(DialogueTreeDto tree)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(tree.DialogueTreeId)) errors.Add("DialogueTreeId is required");
        if (tree.DialogueTreeId?.Length > 100) errors.Add("DialogueTreeId must not exceed 100 characters");
        if (string.IsNullOrWhiteSpace(tree.Title)) errors.Add("Dialogue tree must have a title");
        if (tree.Title?.Length > 200) errors.Add("Title must not exceed 200 characters");
        if (tree.SpeakerTag?.Length > 64) errors.Add("SpeakerTag must not exceed 64 characters");
        if (tree.Nodes is null || tree.Nodes.Count == 0)
        {
            errors.Add("Dialogue tree has no nodes");
            return errors;
        }
        if (tree.Nodes.Any(n => n is null))
        {
            errors.Add("Dialogue tree contains a null node");
            return errors;
        }
        if (tree.Nodes.Any(n => n.Id is null))
        {
            errors.Add("Every node must have an ID");
            return errors;
        }
        foreach (var group in tree.Nodes.GroupBy(n => n.Id))
            if (group.Count() > 1) errors.Add($"Duplicate node ID '{group.Key}' is used by {group.Count()} nodes");
        var nodes = tree.Nodes.GroupBy(n => n.Id).ToDictionary(g => g.Key, g => g.First());
        if (tree.RootNodeId is null || !nodes.TryGetValue(tree.RootNodeId, out DialogueNodeDto? root))
            errors.Add($"Root node '{tree.RootNodeId}' not found in tree");
        else if (root.Type != "Root") errors.Add($"Root node must be of type Root, but is {root.Type}");
        if (!tree.Nodes.Any(n => n.Type == "Root")) errors.Add("Tree must have at least one Root node");

        foreach (DialogueNodeDto node in tree.Nodes)
        {
            string location = $"Node '{node.Id}'";
            if (!Guid.TryParse(node.Id, out Guid nodeId) || nodeId == Guid.Empty) errors.Add($"{location} must have a non-empty GUID ID");
            if (!NodeTypes.Contains(node.Type)) errors.Add($"{location} has unknown type '{node.Type}'");
            if (node.SpeakerTag?.Length > 64) errors.Add($"{location} speaker tag must not exceed 64 characters");
            if (node.Choices is null || node.Conditions is null || node.Actions is null)
            {
                errors.Add($"{location} has null choices, conditions, or actions");
                continue;
            }
            if (node.Type == "Root" && node.Id != tree.RootNodeId && node.Conditions.Count == 0) errors.Add($"{location} greeting needs conditions or must be selected as the default");
            if (node.Type == "Action" && node.Choices.Count != 1) errors.Add($"{location} action must have exactly one continuation");
            if (node.Type == "End" && node.Choices.Count != 0) errors.Add($"{location} ending cannot have replies");
            foreach (var group in node.Choices.Where(c => c != null).GroupBy(c => c.Id))
                if (group.Count() > 1) errors.Add($"{location} has duplicate reply ID '{group.Key}'");
            foreach (DialogueChoiceDto choice in node.Choices)
            {
                if (choice is null) { errors.Add($"{location} contains a null reply"); continue; }
                string reply = $"{location}, reply '{choice.ResponseText}'";
                if (!Guid.TryParse(choice.Id, out Guid id) || id == Guid.Empty) errors.Add($"{reply} must have a non-empty GUID ID");
                if (choice.TargetNodeId is null || !nodes.ContainsKey(choice.TargetNodeId)) errors.Add($"Node '{node.Id}' has choice targeting non-existent node '{choice.TargetNodeId}'");
                if (node.Type != "Action" && !choice.IsContinue && string.IsNullOrWhiteSpace(choice.ResponseText)) errors.Add($"{reply} needs response text or must be a Continue link");
                ValidateConditions(choice.Conditions, reply, errors);
            }
            ValidateConditions(node.Conditions, location, errors);
            foreach (DialogueActionDto action in node.Actions)
            {
                if (action is null) { errors.Add($"{location} contains a null action"); continue; }
                string actionLocation = $"{location}, action {action.ActionType}";
                if (!ActionTypes.Contains(action.ActionType)) { errors.Add($"{actionLocation} is unsupported"); continue; }
                Dictionary<string, string> parameters = action.Parameters ?? [];
                string[] required = action.ActionType switch
                {
                    "StartQuest" or "CompleteQuest" => ["questId"],
                    "SetQuestStage" => ["questId", "stageId"],
                    "GiveItem" or "TakeItem" => ["itemTag"],
                    "GiveGold" or "TakeGold" => ["amount"],
                    "ChangeReputation" => ["factionId", "amount"],
                    "GrantKnowledge" => ["loreId"],
                    "OpenShop" => ["storeResRef"],
                    "SetLocalVariable" => ["variableName", "value"],
                    _ => []
                };
                Require(parameters, required, actionLocation, errors);
                foreach (string key in new[] { "quantity", "stageId", "amount", "bonusMarkUp", "bonusMarkDown" })
                    CheckInteger(parameters, key, key is "quantity" or "stageId" || (key == "amount" && action.ActionType is "GiveGold" or "TakeGold"), actionLocation, errors);
                CheckTarget(parameters, actionLocation, errors);
            }
        }

        HashSet<string> reachable = [];
        Stack<string> pending = new(tree.Nodes.Where(n => n.Type == "Root").Select(n => n.Id));
        while (pending.TryPop(out string? id))
        {
            if (!reachable.Add(id) || !nodes.TryGetValue(id, out DialogueNodeDto? node)) continue;
            foreach (DialogueChoiceDto choice in node.Choices ?? [])
                if (choice?.TargetNodeId != null && nodes.ContainsKey(choice.TargetNodeId)) pending.Push(choice.TargetNodeId);
        }
        foreach (DialogueNodeDto node in tree.Nodes.Where(n => !reachable.Contains(n.Id)))
            errors.Add($"Node '{node.Id}' is not reachable from the root node");
        foreach (DialogueNodeDto start in tree.Nodes.Where(n => n.Type == "Action"))
        {
            HashSet<string> visited = [];
            DialogueNodeDto? node = start;
            while (node?.Type == "Action" && node.Choices?.Count == 1 && node.Choices[0] != null)
            {
                if (!visited.Add(node.Id)) { errors.Add($"Node '{start.Id}' leads to an automatic action cycle"); break; }
                if (node.Choices[0].TargetNodeId is not { } target) break;
                nodes.TryGetValue(target, out node);
            }
        }
        return errors;
    }

    private static void ValidateConditions(List<DialogueConditionDto>? conditions, string location, List<string> errors)
    {
        if (conditions is null) { errors.Add($"{location} has null conditions"); return; }
        foreach (DialogueConditionDto condition in conditions)
        {
            if (condition is null) { errors.Add($"{location} contains a null condition"); continue; }
            string context = $"{location}, condition {condition.Type}";
            if (!ConditionTypes.Contains(condition.Type)) { errors.Add($"{context} is unknown"); continue; }
            Dictionary<string, string> parameters = condition.Parameters ?? [];
            string[] required = condition.Type switch
            {
                "HasItem" => ["itemTag"], "QuestState" => ["questId", "requiredState"],
                "ReputationAbove" => ["factionId", "minScore"], "ReputationBelow" => ["factionId", "maxScore"],
                "HasKnowledge" => ["loreId"], "LocalVariable" => ["variableName", "expectedValue"],
                "Custom" => ["handlerName"], _ => []
            };
            Require(parameters, required, context, errors);
            foreach (string key in new[] { "count", "minScore", "maxScore", "stageId" })
                CheckInteger(parameters, key, key is "count" or "stageId", context, errors);
            if (condition.Type == "QuestState" && parameters.TryGetValue("requiredState", out string? state) && !new[] { "Discovered", "InProgress", "Completed", "Failed", "Abandoned", "Expired", "NotStarted" }.Contains(state))
                errors.Add($"{context} has invalid quest state '{state}'");
            CheckTarget(parameters, context, errors);
        }
    }

    private static void Require(Dictionary<string, string> parameters, string[] keys, string location, List<string> errors)
    {
        foreach (string key in keys)
            if (!parameters.TryGetValue(key, out string? value) || value is null || (key is not ("expectedValue" or "value") && string.IsNullOrWhiteSpace(value)))
                errors.Add($"{location} requires '{key}'");
    }

    private static void CheckInteger(Dictionary<string, string> parameters, string key, bool positive, string location, List<string> errors)
    {
        if (parameters.TryGetValue(key, out string? value) && (!int.TryParse(value, out int number) || (positive && number <= 0)))
            errors.Add($"{location}, '{key}' must be {(positive ? "a positive integer" : "an integer")}");
    }

    private static void CheckTarget(Dictionary<string, string> parameters, string location, List<string> errors)
    {
        if (parameters.TryGetValue("target", out string? target) && target is not ("npc" or "player")) errors.Add($"{location}, target must be npc or player");
    }
}
