using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

public record ResourceNodeDefinition(
    int PlcAppearance,
    ResourceType Type,
    string Tag,
    HarvestContext Requirement,
    HarvestOutput[] Outputs,
    int Uses = 50,
    int BaseHarvestRounds = 0,
    string Name = "",
    string Description = "",
    FloraProperties? FloraProperties = null)
{
    public EconomyQuality GetQualityForArea(AreaDefinition area)
    {
        EconomyQuality baseline = EconomyQuality.Average;

        if (Type is ResourceType.Boulder or ResourceType.Geode or ResourceType.Ore)
        {
            int min = (int)area.Environment.MineralQualityRange.Min;
            int max = (int)area.Environment.MineralQualityRange.Max;
            return (EconomyQuality)(Random.Shared.Next(max - min + 1) + min);
        }

        if (FloraProperties != null)
        {
            if (FloraProperties?.PreferredClimate != area.Environment.Climate)
            {
                baseline = (EconomyQuality)Math.Max((int)EconomyQuality.VeryPoor, (int)baseline - 1);
            }

            int soilQualityLevel = (int)area.Environment.SoilQuality;
            if (soilQualityLevel < (int)FloraProperties!.RequiredSoilQuality)
            {
                baseline = (EconomyQuality)Math.Max((int)EconomyQuality.VeryPoor, (int)baseline - 1);
            }
            else
            {
                int maxQuality = Math.Min((int)EconomyQuality.Excellent, soilQualityLevel + 1);
                int range = maxQuality - (int)baseline + 1;
                baseline = (EconomyQuality)(Random.Shared.Next(range) + (int)baseline);
            }
        }

        return baseline;
    }
}

public record FloraProperties(Climate PreferredClimate, EconomyQuality RequiredSoilQuality);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EconomyQuality
{
    None = NWScript.IP_CONST_QUALITY_UNKOWN,
    VeryPoor = NWScript.IP_CONST_QUALITY_VERY_POOR,
    Poor = NWScript.IP_CONST_QUALITY_POOR,
    BelowAverage = NWScript.IP_CONST_QUALITY_BELOW_AVERAGE,
    Average = NWScript.IP_CONST_QUALITY_AVERAGE,
    AboveAverage = NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE,
    Good = NWScript.IP_CONST_QUALITY_GOOD,
    VeryGood = NWScript.IP_CONST_QUALITY_VERY_GOOD,
    Excellent = NWScript.IP_CONST_QUALITY_EXCELLENT,
    Masterwork = NWScript.IP_CONST_QUALITY_MASTERWORK
}
