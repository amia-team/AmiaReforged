using AmiaReforged.Shared.Dialogue;

namespace AmiaReforged.AdminPanel.Services;

public static class DialogueAuthoring
{
    public static DialogueNodeDto CreateNode(string type) => new() { Id = Guid.NewGuid().ToString(), Type = type };

    public static DialogueChoiceDto AddReply(DialogueNodeDto node, bool isContinue = false)
    {
        DialogueChoiceDto reply = new() { Id = Guid.NewGuid().ToString(), IsContinue = isContinue || node.Type == "Action", SortOrder = node.Choices.Count };
        node.Choices.Add(reply);
        return reply;
    }

    public static void RemoveNode(DialogueTreeDto tree, DialogueNodeDto node)
    {
        tree.Nodes.Remove(node);
        foreach (DialogueNodeDto survivor in tree.Nodes)
        {
            survivor.Choices.RemoveAll(c => c.TargetNodeId == node.Id);
            if (survivor.ParentNodeId == node.Id) survivor.ParentNodeId = null;
        }
        if (tree.RootNodeId == node.Id) tree.RootNodeId = tree.Nodes.Where(n => n.Type == "Root").OrderBy(n => n.SortOrder).FirstOrDefault()?.Id;
    }

    public static void MoveReply(DialogueNodeDto node, DialogueChoiceDto reply, int offset)
    {
        List<DialogueChoiceDto> sorted = node.Choices.OrderBy(c => c.SortOrder).ToList();
        int index = sorted.IndexOf(reply);
        int destination = index + offset;
        if (index < 0 || destination < 0 || destination >= sorted.Count) return;
        (sorted[index], sorted[destination]) = (sorted[destination], sorted[index]);
        for (int i = 0; i < sorted.Count; i++) sorted[i].SortOrder = i;
    }
}
