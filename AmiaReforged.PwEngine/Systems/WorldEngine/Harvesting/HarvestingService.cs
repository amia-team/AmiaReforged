using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

[ServiceBinding(typeof(HarvestingService))]
public class HarvestingService(
    IResourceNodeInstanceRepository repository,
    IItemDefinitionRepository itemDefinitionRepository) : IHarvestProcessor
{
    private readonly Dictionary<Guid, SpawnedNode> _spawnedNodes = new();

    public void RegisterPlaceable(NwPlaceable placeable, ResourceNodeInstance instance)
    {
        _spawnedNodes.Add(placeable.UUID, new SpawnedNode(placeable, instance));

        switch (instance.Definition.Type)
        {
            case ResourceType.Undefined:
                break;
            case ResourceType.Ore:
                placeable.OnPhysicalAttacked += HandleAttackedHarvest;
                break;
            case ResourceType.Geode:
                break;
            case ResourceType.Boulder:
                break;
            case ResourceType.Tree:
                break;
            case ResourceType.Flora:
                break;
        }

        instance.OnHarvest += HandleHarvest;
        repository.AddNodeInstance(instance);
    }

    private void HandleAttackedHarvest(PlaceableEvents.OnPhysicalAttacked obj)
    {
        SpawnedNode? node = _spawnedNodes.GetValueOrDefault(obj.Placeable.UUID);

        if (node is null) return;

        NwPlaceable? plc = node.Placeable;

        if (plc is null || !plc.IsValid) return;

        // node.Instance.Harvest();
    }

    public void RegisterNode(ResourceNodeInstance instance)
    {
        instance.OnHarvest += HandleHarvest;
        instance.OnDestroyed += Delete;
        repository.AddNodeInstance(instance);
        repository.SaveChanges();
    }

    public List<ResourceNodeInstance> GetInstancesForArea(string areaRef)
    {
        return repository.GetInstancesByArea(areaRef);
    }

    private void Delete(ResourceNodeInstance instance)
    {
        SpawnedNode? node = _spawnedNodes.GetValueOrDefault(instance.Id);

        if (node is null) return;

        NwPlaceable? plc = node.Placeable;

        repository.Delete(instance);
        repository.SaveChanges();

        if (plc is null || !plc.IsValid)
        {
            plc?.Destroy();
        }

        _spawnedNodes.Remove(instance.Id);
    }

    private void HandleHarvest(HarvestEventData data)
    {
        IEnumerable<HarvestOutput> outputs = data.NodeInstance.Definition.Outputs;

        List<KnowledgeHarvestEffect> applicable =
            data.Character.KnowledgeEffectsForResource(data.NodeInstance.Definition.Tag);

        foreach (HarvestOutput harvestOutput in outputs)
        {
            ItemDefinition? definition = itemDefinitionRepository.GetByTag(harvestOutput.ItemDefinitionTag);

            if (definition == null) continue;

            IPQuality quality = data.NodeInstance.Quality;
            int totalQuality = (int)quality;

            List<KnowledgeHarvestEffect> qualityImprovements =
                applicable.Where(he => he.StepModified == HarvestStep.Quality).ToList();
            foreach (KnowledgeHarvestEffect qualityImprovement in qualityImprovements)
            {
                switch (qualityImprovement.Operation)
                {
                    case EffectOperation.Additive:
                        totalQuality += (int)qualityImprovement.Value;
                        break;
                    case EffectOperation.PercentMult:
                        totalQuality += totalQuality * (int)qualityImprovement.Value;
                        break;
                }
            }

            totalQuality = Math.Max(NWScript.IP_CONST_QUALITY_VERY_POOR,
                Math.Min(NWScript.IP_CONST_QUALITY_MASTERWORK, totalQuality));

            int totalQuantity = (int)harvestOutput.Quantity;
            List<KnowledgeHarvestEffect> yieldImprovements =
                applicable.Where(he => he.StepModified == HarvestStep.ItemYield).ToList();
            foreach (KnowledgeHarvestEffect yieldImprovement in yieldImprovements)
            {
                switch (yieldImprovement.Operation)
                {
                    case EffectOperation.Additive:
                        totalQuantity += (int)yieldImprovement.Value;
                        break;
                    case EffectOperation.PercentMult:
                        totalQuantity += totalQuantity * (int)yieldImprovement.Value;
                        break;
                }
            }

            ItemDto dto = new(definition, (IPQuality)totalQuality, (IPQuality)totalQuality);

            for (int i = 0; i < totalQuantity; i++)
            {
                data.Character.AddItem(dto);
            }
        }

        data.NodeInstance.Uses -= 1;

        repository.Update(data.NodeInstance);
    }
}

public record SpawnedNode(NwPlaceable? Placeable, ResourceNodeInstance Instance);

public interface ICharacterManager
{
    ICharacter? GetCharacter(Guid id);
}
