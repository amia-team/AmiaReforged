using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEdit;

internal record LocalVariableData
{
    public LocalVariableType Type { get; init; }
    public int IntValue { get; init; }
    public float FloatValue { get; init; }
    public string StringValue { get; init; } = string.Empty;
    public Location? LocationValue { get; init; }
    public NwObject? ObjectValue { get; init; }
}

internal enum LocalVariableType
{
    Int,
    Float,
    String,
    Location,
    Object
}
