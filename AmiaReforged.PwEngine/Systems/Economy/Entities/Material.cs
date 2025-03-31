using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.Economy.Entities;

public class Material
{
    public MaterialEnum MaterialType { get; set; }
    public float CostModifier { get; set;  }
    public float DurabilityModifier { get; set; }
    public float MagicModifier { get; set; }
    public float WeightModifier { get; set; }
}