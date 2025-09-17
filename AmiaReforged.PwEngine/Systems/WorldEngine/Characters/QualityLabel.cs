using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public static class QualityLabel
{
    public static string ToQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Deficient",
        NWScript.IP_CONST_QUALITY_POOR => "Inferior",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Flawed",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Fine",
        NWScript.IP_CONST_QUALITY_GOOD => "Very Fine",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Superior",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Exceptional",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Masterwork",
        _ => ""
    };
}