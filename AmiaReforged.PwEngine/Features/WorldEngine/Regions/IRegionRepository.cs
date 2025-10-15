namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

public interface IRegionRepository
{
    void Add(RegionDefinition definition);
    void Update(RegionDefinition definition);
    bool Exists(string tag);
    List<RegionDefinition> All();
}

