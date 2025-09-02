using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;

public class InMemoryResourceNodeInstanceRepository : IResourceNodeInstanceRepository
{
    private readonly List<ResourceNodeInstance> _resourceNodeInstances = [];


    public void AddNodeInstance(ResourceNodeInstance instance)
    {
        _resourceNodeInstances.Add(instance);
    }

    public void RemoveNodeInstance(ResourceNodeInstance instance)
    {
        _resourceNodeInstances.Remove(instance);
    }

    public void Update(ResourceNodeInstance dataNodeInstance)
    {
        _resourceNodeInstances.Remove(dataNodeInstance);
        _resourceNodeInstances.Add(dataNodeInstance);
    }

    public List<ResourceNodeInstance> GetInstances()
    {
        return _resourceNodeInstances;
    }

    public List<ResourceNodeInstance> GetInstancesByArea(string resRef)
    {
        return _resourceNodeInstances.Where(r => r.Area == resRef).ToList();
    }
}
