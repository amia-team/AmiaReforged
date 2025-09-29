namespace AmiaReforged.PwEngine.Systems.WorldEngine.Regions;

public class Realm
{
    public required string Tag { get; set; }
    public required string Name { get; set; }
    public required GovernmentType Government { get; set; }
    public List<RegionDefinition> Territories { get; set; } = [];
}
