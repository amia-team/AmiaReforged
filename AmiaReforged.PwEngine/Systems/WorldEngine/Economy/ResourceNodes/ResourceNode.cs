using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.ResourceNodes;

public class ResourceNodeDefinition
{
    /// <summary>
    /// Required name field. What the player sees when they mouse over instances of this resource node's placeables.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Sets the tag so that any special edge cases can be handled by tag-based scripting, later.
    /// </summary>
    public required string Tag { get; set; }

    /// <summary>
    /// Optional field for setting a description for job system nodes.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Appearance.2da entry that determines how the node looks in game
    /// </summary>
    public required int Appearance { get; set; }

    /// <summary>
    /// Optional variation in size to create a more varied look to duplicate nodes.
    /// </summary>
    public float ScaleVariance { get; set; } = 0.0f;

    /// <summary>
    /// Amount of time in rounds it takes to complete one harvest cycle.
    /// </summary>
    public required int HarvestTime { get; set; }

    public ResourceType Type { get; set; }
}

public class ResourceNode
{
    public string ResRef { get; set; }
    public required SavedLocation Location { get; set; }
    public float BaseRichnessWeight { get; set; }
    public float Richness { get; set; }
}

public interface IHarvestable
{
    bool CanHarvest(CharacterHarvestContext context);
    IEnumerable<RawGood> Harvest(CharacterHarvestContext context);
}

public class OreNode : ResourceNode, IHarvestable
{
    public bool CanHarvest(CharacterHarvestContext context)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<RawGood> Harvest(CharacterHarvestContext context)
    {
        throw new NotImplementedException();
    }
}

public class RawGood
{
}

public class CharacterHarvestContext : IActionContext
{
}

public interface IActionContext
{
}