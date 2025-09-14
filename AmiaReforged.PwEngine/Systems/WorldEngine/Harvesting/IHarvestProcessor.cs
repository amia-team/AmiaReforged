using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface IHarvestProcessor
{
    void RegisterNode(ResourceNodeInstance instance);
}