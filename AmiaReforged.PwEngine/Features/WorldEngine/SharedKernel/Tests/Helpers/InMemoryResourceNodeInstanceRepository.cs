using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

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

    public bool SaveChanges()
    {
        // Does nothing here
        return true;
    }

    public void Delete(ResourceNodeInstance instance)
    {
        _resourceNodeInstances.Remove(instance);
    }
}
