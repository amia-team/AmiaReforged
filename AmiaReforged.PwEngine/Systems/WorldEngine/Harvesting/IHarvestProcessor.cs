using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface IHarvestProcessor
{
    void RegisterNode(ResourceNodeInstance instance);

    List<ResourceNodeInstance> GetInstancesForArea(string areaRef);

    void Delete(ResourceNodeInstance instance);
}
