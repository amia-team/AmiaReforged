namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

internal record ItemDataRecord(
    string Name,
    string Description,
    string Tag,
    Dictionary<string, LocalVariableData> Variables);
