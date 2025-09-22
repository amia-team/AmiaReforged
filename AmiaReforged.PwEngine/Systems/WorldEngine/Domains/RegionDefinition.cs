namespace AmiaReforged.PwEngine.Systems.WorldEngine.Domains;

public class RegionDefinition
{
    public required string Tag { get; set; }
    public required string Name { get; set; }
    public List<AreaDefinition> Areas { get; set; } = [];
}
