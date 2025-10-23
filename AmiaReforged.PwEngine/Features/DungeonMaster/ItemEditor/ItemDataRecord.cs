namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEdit;

internal record ItemData(
    string Name,
    string Description,
    string Tag,
    Dictionary<string, LocalVariableData> Variables);
