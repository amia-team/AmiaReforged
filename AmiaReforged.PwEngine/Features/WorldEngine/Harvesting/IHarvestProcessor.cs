using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;

public interface IHarvestProcessor
{
    void RegisterNode(ResourceNodeInstance instance);

    List<ResourceNodeInstance> GetInstancesForArea(string areaRef);

    void Delete(ResourceNodeInstance instance);
    void ClearNodes(string areaResRef);
}
