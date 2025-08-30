using System.Numerics;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public class ResourceNodeInstance
{
    public delegate void OnHarvestHandler(HarvestEventData data);

    public event OnHarvestHandler? OnHarvest;

    public long Id { get; set; }
    public required string Area { get; set; }

    public required ResourceNodeDefinition Definition { get; set; }
    public int Uses { get; set; }
    public QualityLevel Quality { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }
    private int HarvestProgress { get; set; }


    public Location? ToLocation()
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

        HarvestProgress++;

        if (HarvestProgress < Definition.BaseHarvestRounds)
        {
            return HarvestResult.InProgress;
        }

        HarvestEventData data = new(character, this);
        OnHarvest?.Invoke(data);

        HarvestProgress = 0;
        return HarvestResult.Finished;
    }
}

public enum HarvestResult
{
    Finished,
    InProgress,
    NoTool,
    Error
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
