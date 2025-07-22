using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.ResourceNodes;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;

public class NodeDefinition
{
    public required string Name { get; set; }
    public required string Tag { get; set; }

    public required string Description { get; set; }

    public List<ItemYieldDescription> Yield { get; set; } = new();
    public ResourceType Type { get; set; }
    public int Appearance { get; set; } = 1;
}

public class ResourceNodeInstance
{
    public required ResourceNodeDefinition Definition { get; set; }
    public NwPlaceable? Instance { get; set; }
    public required SavedLocation Location { get; set; }
    public float Richness { get; set; }
}

public class ItemYieldDescription
{
    public ItemType ItemType { get; set; }

    public MaterialEnum Material { get; set; }

    /// <summary>
    ///  A floating point scale from 0.0 to 1.0 that determines the chance of an item appearing after a harvest cycle.
    /// </summary>
    public float Chance { get; set; }

    public QualityEnum QualityMin { get; set; }
    public QualityEnum QualityMax { get; set; }

    public QualityEnum? QualityMinOverride { get; set; }
    public QualityEnum? QualityMaxOverride { get; set; }
}

public class ItemDefinition
{
}