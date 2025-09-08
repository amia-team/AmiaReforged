namespace AmiaReforged.PwEngine.Systems.WorldEngine.Domains;

public interface IRegionRepository
{
    void Add(RegionDefinition definition);
    void Update(RegionDefinition definition);
    bool Exists(string tag);
    List<RegionDefinition> All();
}
