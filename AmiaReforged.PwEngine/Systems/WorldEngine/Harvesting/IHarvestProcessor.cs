using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface IHarvestProcessor
{
    void RegisterNode(ResourceNodeInstance instance);

    List<ResourceNodeInstance> GetInstancesForArea(string areaRef);

    void Delete(ResourceNodeInstance instance);
    void ClearNodes(string areaResRef);
}
