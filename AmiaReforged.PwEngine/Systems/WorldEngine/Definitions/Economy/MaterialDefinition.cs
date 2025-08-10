using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;

public class MaterialDefinition
{
    public MaterialEnum MaterialType { get; set; }
    public float CostModifier { get; set;  }
    public float DurabilityModifier { get; set; }
    public float MagicModifier { get; set; }
    public float WeightModifier { get; set; }
}