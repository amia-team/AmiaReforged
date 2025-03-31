using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Economy.Entities;

public class CultivatedResource
{
    /// <summary>
    /// The NWN item type of the resource.
    /// </summary>
    public BaseItemType ItemType { get; set; }

    /// <summary>
    /// The economy item type of the resource.
    /// </summary>
    public ItemType Type { get; set; }
    
    public string Name { get; set; }

    /// <summary>
    /// Time to harvest in rounds.
    /// </summary>
    public int TimeToHarvest { get; set; }
    
    public List<string> EnvironmentTags { get; set; }
    
    public List<EnvironmentTrait> SuitableEnvironments { get; set; } = new();
}