using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;

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
