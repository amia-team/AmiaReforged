using System.Numerics;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

public class ResourceNodeInstance
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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

    /// <summary>
    /// Increment harvest progress and return the new value
    /// </summary>
    public int IncrementHarvestProgress(int amount)
    {
        HarvestProgress += amount;
        return HarvestProgress;
    }

    /// <summary>
    /// Reset harvest progress to zero
    /// </summary>
    public void ResetHarvestProgress()
    {
        HarvestProgress = 0;
    }

    /// <summary>
    /// Decrement remaining uses
    /// </summary>
    public void DecrementUses()
    {
        Uses--;
    }


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
}
