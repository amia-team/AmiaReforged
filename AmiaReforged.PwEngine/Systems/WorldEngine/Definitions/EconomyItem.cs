using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public record EconomyItem
{
    public required string BaseItemResRef { set; get; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Tag { get; set; }
    public int Appearance { get; set; } = 1;
}



public class EconomyItemInstance
{
    public required EconomyItem Definition { get; set; }
    /// <summary>
    /// Instances without an item property denoting quality get theirs set to average.
    /// </summary>
    public QualityEnum Quality { get; set; } = QualityEnum.Average;

    /// <summary>
    /// '0' means no known maker.
    /// </summary>
    public long CharacterId { get; set; } = 0;

    /// <summary>
    /// How 'rich' the resource is. Impacts how many extra items get created when processed at the appropriate workshop.
    /// Only really applies to raw goods.
    /// </summary>
    public float Richess { get; set; } = 1.0f;
}
