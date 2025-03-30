using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.Economy.Entities;

public class Material
{
    public const float AdamantineCostModifier = 2.0f;
    public const float DiamondCostModifier = 2.1f;
    public const float GoldCostModifier = 1.9f;
    public const float IronCostModifier = 1.0f;
    public const float MithralCostModifier = 1.96f;
    public const float SilverCostModifier = 1.8f;

    private Material(MaterialEnum type, float costModifier)
    {
        Type = type;
        CostModifier = costModifier;
    }
    
    public MaterialEnum Type { get; }
    public float CostModifier { get; }

    public static Material FromType(MaterialEnum type)
    {
        return type switch
        {
            MaterialEnum.Adamantine => new Material(type, AdamantineCostModifier),
            MaterialEnum.Diamond => new Material(type, DiamondCostModifier),
            MaterialEnum.Gold => new Material(type, 1.3f),
            MaterialEnum.Iron => new Material(type, 1.0f),
            MaterialEnum.Mithral => new Material(type, 1.4f),
            MaterialEnum.Silver => new Material(type, 1.1f),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}