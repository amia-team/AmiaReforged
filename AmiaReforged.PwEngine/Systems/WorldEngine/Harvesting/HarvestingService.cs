using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

// [ServiceBinding(typeof(HarvestingService))]
public class HarvestingService(IResourceNodeInstanceRepository repository, IItemDefinitionRepository itemDefinitionRepository) : IHarvestProcessor
{
    public void RegisterNode(ResourceNodeInstance instance)
    {
        instance.OnHarvest += HandleHarvest;
        repository.AddNodeInstance(instance);
    }

    private void HandleHarvest(HarvestEventData data)
    {
        IEnumerable<HarvestOutput> outputs = data.NodeInstance.Definition.Outputs;

        foreach (HarvestOutput harvestOutput in outputs)
        {
            ItemDefinition? definition = itemDefinitionRepository.GetByTag(harvestOutput.ItemDefinitionTag);

            if(definition == null) continue;

            // TODO: calculations/determination for quality...
            ItemDto dto = new ItemDto(definition, IPQuality.Average, harvestOutput.Quantity);

            data.Character.AddItem(dto);
        }

        data.NodeInstance.Uses -= 1;

        repository.Update(data.NodeInstance);
    }
}

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemDefinition definition);
    ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag);
}

public interface IResourceNodeInstanceRepository
{
    void AddNodeInstance(ResourceNodeInstance instance);
    void RemoveNodeInstance(ResourceNodeInstance instance);
    List<ResourceNodeInstance> GetInstances();
    List<ResourceNodeInstance> GetInstancesByArea(string resRef);
    void Update(ResourceNodeInstance dataNodeInstance);
}

public interface IHarvestProcessor
{
    void RegisterNode(ResourceNodeInstance instance);
}
