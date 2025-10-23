namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

internal record ItemData(
    string Name,
    string Description,
    string Tag,
    Dictionary<string, LocalVariableData> Variables);
