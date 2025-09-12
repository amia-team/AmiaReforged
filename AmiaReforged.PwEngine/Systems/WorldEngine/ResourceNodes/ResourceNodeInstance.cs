using System.Numerics;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public class ResourceNodeInstance
{
    public delegate void OnHarvestHandler(HarvestEventData data);
    public delegate void OnDestroyedHandler(ResourceNodeInstance instance);

    public event OnHarvestHandler? OnHarvest;
    public event OnDestroyedHandler? OnDestroyed;


    public long Id { get; set; }
    public required string Area { get; set; }

    public required ResourceNodeDefinition Definition { get; set; }
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

    public HarvestResult Harvest(ICharacter character)
    {
        ItemSnapshot tool = character.GetEquipment()[EquipmentSlots.RightHand];
        if (Definition.Requirement.RequiredItemType != JobSystemItemType.None &&
            tool.Type != Definition.Requirement.RequiredItemType)
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

public enum HarvestResult
{
    Finished,
    InProgress,
    NoTool
}

public record HarvestEventData(ICharacter Character, ResourceNodeInstance NodeInstance);

public enum QualityLevel
{
    VeryPoor = -2,
    Poor = -1,
    BelowAverage = 0,
    Average = 1,
    AboveAverage = 2,
    Good = 3,
    VeryGood = 4,
    Excellent = 5,
    Masterwork = 6
}
