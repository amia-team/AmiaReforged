using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;

public class NodeDefinition
{
    public required string Name { get; set; }
    public required string Tag { get; set; }
    
    public required string Description { get; set; }
    
    public List<ItemYieldDescription> Yield { get; set; } = new();
    
}

public class ItemYieldDescription
{
    public ItemType ItemType { get; set; }
    
    public MaterialEnum Material { get; set; }
    public float Chance { get; set; }
    
    public QualityEnum QualityMin { get; set; }
    public QualityEnum QualityMax { get; set; }
    
    public QualityEnum? QualityMinOverride { get; set; }
    public QualityEnum? QualityMaxOverride { get; set; }
    
}

public class ItemDefinition
{
}