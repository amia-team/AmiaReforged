using System.Numerics;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;

public class ResourceNodeInstance
{
    public delegate void OnHarvestHandler(HarvestEventData data);

    public delegate void OnDestroyedHandler(ResourceNodeInstance instance);


    public event OnHarvestHandler? OnHarvest;
    public event OnDestroyedHandler? OnDestroyed;


    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Area { get; set; }

    public required ResourceNodeDefinition Definition { get; init; }
    public int Uses { get; set; }
    public IPQuality Quality { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }
    private int HarvestProgress { get; set; }


    public Location? GameLocation()
    {
        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == Area);

        return area != null ? Location.Create(area, new Vector3(X, Y, Z), Rotation) : null;
    }

    public void Destroy()
    {
        LogManager.GetCurrentClassLogger().Info("Destroy called");
        OnDestroyed?.Invoke(this);
    }

    public HarvestResult Harvest(ICharacter character)
    {
        ItemSnapshot? tool = character.GetEquipment()[EquipmentSlots.RightHand];
        if (Definition.Requirement.RequiredItemType != JobSystemItemType.None &&
            tool?.Type != Definition.Requirement.RequiredItemType)
        {
            return HarvestResult.NoTool;
        }

        int harvestProgressMod = 0;

        foreach (KnowledgeHarvestEffect effect in character.KnowledgeEffectsForResource(Definition.Tag)
                     .Where(r => r.StepModified == HarvestStep.HarvestStepRate))
        {
            switch (effect.Operation)
            {
                case EffectOperation.Additive:
                    harvestProgressMod += (int)effect.Value;
                    break;
            }
        }

        HarvestProgress = 1 + HarvestProgress + harvestProgressMod;

        if (HarvestProgress < Definition.BaseHarvestRounds)
        {
            return HarvestResult.InProgress;
        }

        HarvestEventData data = new(character, this);
        OnHarvest?.Invoke(data);
        Uses--;

        HarvestProgress = 0;

        if (Uses <= 0)
        {
            OnDestroyed?.Invoke(this);
        }

        return HarvestResult.Finished;
    }
}
