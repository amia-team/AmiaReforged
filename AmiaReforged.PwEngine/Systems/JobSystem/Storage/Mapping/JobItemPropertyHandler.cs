using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

[ServiceBinding(typeof(JobItemPropertyHandler))]
public class JobItemPropertyHandler
{
    // public float DurabilityFromItem(NwItem item)
    // {
    //
    // }

    public float DeriveDurability(QualityEnum qualityEnum, MaterialEnum materialEnum) =>
        DurabilityFromQuality(qualityEnum) + DurabilityFromMaterial(materialEnum);

    private float DurabilityFromQuality(QualityEnum qualityEnum)
    {
        return qualityEnum switch
        {
            QualityEnum.Cut => 0.1f,
            QualityEnum.Raw => 0.1f,
            QualityEnum.VeryPoor => -0.4f,
            QualityEnum.Poor => -0.3f,
            QualityEnum.BelowAverage => 0.0f,
            QualityEnum.Average => 0.4f,
            QualityEnum.AboveAverage => 0.7f,
            QualityEnum.Good => 0.8f,
            QualityEnum.VeryGood => 0.9f,
            QualityEnum.Excellent => 1.0f,
            QualityEnum.Masterwork => 1.1f,
            _ => 0.6f
        };
    }

    private float DurabilityFromMaterial(MaterialEnum materialEnum)
    {
        return materialEnum switch
        {
            MaterialEnum.Darksteel => 1.5f,
            MaterialEnum.Adamantine => 2.3f,
            MaterialEnum.Iron => 1.0f,
            MaterialEnum.Cold_Iron => 1.0f,
            MaterialEnum.Steel => 1.2f,
            MaterialEnum.Bronze => 1.1f,
            MaterialEnum.Copper => 1.0f,
            MaterialEnum.Wood => 0.8f,
            MaterialEnum.Brass => 0.5f,
            MaterialEnum.Silver_Alchemical => 1.0f,
            MaterialEnum.Gold => -0.2f,
            MaterialEnum.Silver => -0.3f,
            MaterialEnum.Wood_Ash => 1.0f,
            MaterialEnum.Wood_Oak => 0.9f,
            MaterialEnum.Wood_Cedar => 0.8f,
            MaterialEnum.Wood_Pine => 0.8f,
            MaterialEnum.Wood_Yew => 0.8f,
            MaterialEnum.Wood_Duskwood => 0.8f,
            MaterialEnum.Wood_Ironwood => 1.0f,
            MaterialEnum.Wood_Darkwood_Zalant => 0.85f,
            _ => 0.0f
        };
    }

    private float MagicFromMaterial(MaterialEnum materialEnum)
    {
        return materialEnum switch
        {
            // Metals
            MaterialEnum.Darksteel => 1.1f,
            MaterialEnum.Adamantine => 1.0f,
            MaterialEnum.Gold => 1.0f,
            MaterialEnum.Silver_Alchemical => 1.0f,
            MaterialEnum.Platinum => 1.0f,
            MaterialEnum.Silver => 0.8f,
            MaterialEnum.Cold_Iron => 0.8f,
            // Wood
            MaterialEnum.Wood_Darkwood_Zalant => 1.0f,
            // Dragon hides
            MaterialEnum.Hide_Dragon_Black => 1.2f,
            MaterialEnum.Hide_Dragon_Blue => 1.3f,
            MaterialEnum.Hide_Dragon_Green => 1.1f,
            MaterialEnum.Hide_Dragon_Red => 1.4f,
            MaterialEnum.Hide_Dragon_White => 1.1f,
            MaterialEnum.Hide_Dragon_Brass => 1.1f,
            MaterialEnum.Hide_Dragon_Bronze => 1.2f,
            MaterialEnum.Hide_Dragon_Copper => 1.1f,
            MaterialEnum.Hide_Dragon_Gold => 1.4f,
            MaterialEnum.Hide_Dragon_Silver => 1.3f,
            _ => 0.0f
        };
    }

    public int DeriveMagic(QualityEnum quality, MaterialEnum material)
    {
        float magicFromMaterial = MagicFromMaterial(material);
        if (magicFromMaterial == 0.0f) return 0;

        return (int)(magicFromMaterial + MagicFromQuality(quality));
    }

    private static float MagicFromQuality(QualityEnum quality)
    {
        return quality switch
        {
            QualityEnum.VeryPoor => 0.1f,
            QualityEnum.Poor => 0.2f,
            QualityEnum.BelowAverage => 0.3f,
            QualityEnum.Average => 0.4f,
            QualityEnum.AboveAverage => 0.5f,
            QualityEnum.Good => 0.6f,
            QualityEnum.VeryGood => 0.7f,
            QualityEnum.Masterwork => 0.8f,
            _ => 0.0f
        };
    }

    public int DeriveValue(QualityEnum quality, MaterialEnum material) => 0;

    public ItemType DeriveType(BaseItemType baseItemItemType) => ItemType.Log;
}
