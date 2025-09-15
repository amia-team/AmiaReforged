using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface IResourceNodeInstanceRepository
{
    void AddNodeInstance(ResourceNodeInstance instance);
    void RemoveNodeInstance(ResourceNodeInstance instance);
    List<ResourceNodeInstance> GetInstances();
    List<ResourceNodeInstance> GetInstancesByArea(string resRef);
    void Update(ResourceNodeInstance dataNodeInstance);
    bool SaveChanges();
    void Delete(ResourceNodeInstance instance);
}
