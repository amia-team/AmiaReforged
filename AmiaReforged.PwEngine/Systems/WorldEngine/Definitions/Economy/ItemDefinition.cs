using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;

public class ItemDefinition
{
    public required string BaseItemResRef { get; set; }
    public required string Tag { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int Appearance { get; set; }
    public QualityEnum MaxQuality { get; set; } = QualityEnum.Undefined;
    public QualityEnum MinQuality { get; set; } = QualityEnum.Undefined;
    public ItemType ItemType { get; set; }
}
