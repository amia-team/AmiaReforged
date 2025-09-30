using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

[ServiceBinding(typeof(IHarvestProcessor))]
public class HarvestingService(
    IResourceNodeInstanceRepository repository,
    IItemDefinitionRepository itemDefinitionRepository) : IHarvestProcessor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, List<ResourceNodeInstance>> _nodeCache = new();

    public void RegisterNode(ResourceNodeInstance instance)
    {
        instance.OnHarvest += HandleHarvest;
        instance.OnDestroyed += Delete;

        if (!_nodeCache.ContainsKey(instance.Area))
        {
            _nodeCache.TryAdd(instance.Area, []);
        }

        _nodeCache[instance.Area].Add(instance);

        repository.AddNodeInstance(instance);
        repository.SaveChanges();
    }

    public void ClearNodes(string areaResRef)
    {
        List<ResourceNodeInstance> snapshot = new List<ResourceNodeInstance>(GetInstancesForArea(areaResRef));

        foreach (ResourceNodeInstance instance in snapshot)
        {
            instance.Destroy();
        }

        _nodeCache.Remove(areaResRef);
    }

    public List<ResourceNodeInstance> GetInstancesForArea(string areaRef)
    {
        List<ResourceNodeInstance>? resourceNodeInstances = _nodeCache.GetValueOrDefault(areaRef);
        return resourceNodeInstances ?? [];
    }

    public void Delete(ResourceNodeInstance instance)
    {
        instance.OnHarvest -= HandleHarvest;
        instance.OnDestroyed -= Delete;

        repository.Delete(instance);
        bool success = _nodeCache[instance.Area].Remove(instance);
        if (!success)
        {
            Log.Error($"Failed to delete instance from {instance.Area}");
        }

        repository.SaveChanges();
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

            if (harvestOutput.Chance < 100)
            {
                int randomPercentage = NWScript.Random(100);
                if (randomPercentage >= harvestOutput.Chance)
                {
                    continue;
                }
            }

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

            int totalQuantity = harvestOutput.Quantity;
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

        repository.Update(data.NodeInstance);
        repository.SaveChanges();
    }
}
