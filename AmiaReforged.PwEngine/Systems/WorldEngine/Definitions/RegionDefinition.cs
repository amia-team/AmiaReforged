namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class RegionDefinition
{
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required string ClimateTag { get; set; }
    public ClimateDefinition? Climate { get; set; }
    public List<AreaDefinition> Areas { get; set; } = [];
}
