using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

[ServiceBinding(typeof(IHarvestProcessor))]
public class HarvestingService(
    IResourceNodeInstanceRepository repository,
    IItemDefinitionRepository itemDefinitionRepository) : IHarvestProcessor
{
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

    public void Delete(ResourceNodeInstance instance)
    {
        repository.Delete(instance);
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
