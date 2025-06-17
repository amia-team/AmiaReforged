namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public class Region
{
    public required string Name { get; set; }
    public required ClimateDefinition ClimateDefinition { get; set; }
    public List<Area> Areas { get; set; } = [];
}