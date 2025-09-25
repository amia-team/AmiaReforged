using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobSystemItemType
{
    None = 0,
    ToolPick = 1,
    ToolHammer = 2,
    ToolAxe = 3,
    ToolFroe = 4,
    ToolAdze = 5,
    ToolWoodChisel = 6,
    ToolMasonChisle = 7,
    ResourceOre = 8,
    ResourceStone = 9,
    ResourceLog = 10,
    ResourcePlank = 11,
    ResourceBrick = 12,
    ResourceIngot = 13
}
