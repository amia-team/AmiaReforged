using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

/// <summary>
/// Maps from NWN Item properties to Job System item properties and vice versa.
/// </summary>
[ServiceBinding(typeof(QualityPropertyMapper))]
public class QualityPropertyMapper : IMappingService<QualityEnum, IPQuality>
{
    public QualityEnum MapFrom(IPQuality quality)
    {
        return quality switch
        {
            IPQuality.Cut => QualityEnum.Cut,
            IPQuality.Raw => QualityEnum.Raw,
            IPQuality.VeryPoor => QualityEnum.VeryPoor,
            IPQuality.Poor => QualityEnum.Poor,
            IPQuality.BelowAverage => QualityEnum.BelowAverage,
            IPQuality.Average => QualityEnum.Average,
            IPQuality.AboveAverage => QualityEnum.AboveAverage,
            IPQuality.Good => QualityEnum.Good,
            IPQuality.VeryGood => QualityEnum.VeryGood,
            IPQuality.Excellent => QualityEnum.Excellent,
            IPQuality.Masterwork => QualityEnum.Masterwork,
            _ => QualityEnum.Average
        };
    }

    public IPQuality MapTo(QualityEnum item)
    {
        return item switch
        {
            QualityEnum.Cut => IPQuality.Cut,
            QualityEnum.Raw => IPQuality.Raw,
            QualityEnum.VeryPoor => IPQuality.VeryPoor,
            QualityEnum.Poor => IPQuality.Poor,
            QualityEnum.BelowAverage => IPQuality.BelowAverage,
            QualityEnum.Average => IPQuality.Average,
            QualityEnum.AboveAverage => IPQuality.AboveAverage,
            QualityEnum.Good => IPQuality.Good,
            QualityEnum.VeryGood => IPQuality.VeryGood,
            QualityEnum.Excellent => IPQuality.Excellent,
            QualityEnum.Masterwork => IPQuality.Masterwork,
            _ => IPQuality.Average
        };
    }
}