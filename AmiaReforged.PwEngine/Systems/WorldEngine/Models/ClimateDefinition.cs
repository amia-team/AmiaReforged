namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public class ClimateDefinition
{
    public required string Name { get; set; }
    public required string Description { get; set; }

    /// <summary>
    /// An empty whitelist means nothing will spawn. Otherwise, a list of resources that may potentially spawn in this environment.
    /// However, note that an area will still need to have areas set up for certain resource types to spawn.
    /// This simply prevents things like Adamant from spawning in Southport's farmland.
    /// </summary>
    public List<string> WhitelistedNodeTags { get; set; }
    // public List<string> WhitelistedCritters { get; set; } = [];
    
}