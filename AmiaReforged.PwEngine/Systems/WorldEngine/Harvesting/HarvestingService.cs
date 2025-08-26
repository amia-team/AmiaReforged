using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

// [ServiceBinding(typeof(HarvestingService))]
public class HarvestingService(IResourceNodeInstanceRepository repository) : IHarvestProcessor
{
    public void RegisterNode(ResourceNodeInstance instance)
    {
        throw new NotImplementedException();
    }
}

public interface IResourceNodeInstanceRepository
{
    void AddNodeInstance(ResourceNodeInstance instance);
    void RemoveNodeInstance(ResourceNodeInstance instance);
    List<ResourceNodeInstance> GetInstances();
    List<ResourceNodeInstance> GetInstancesByArea(string resRef);
}

public interface IHarvestProcessor
{
    void RegisterNode(ResourceNodeInstance instance);
}
