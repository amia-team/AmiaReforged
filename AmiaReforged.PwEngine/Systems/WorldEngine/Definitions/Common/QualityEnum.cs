using System.Text.RegularExpressions;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

public enum QualityEnum
{
    Undefined,
    Cut = -2,
    Raw = -1,
    None = 0,
    VeryPoor = 1,
    Poor = 2,
    BelowAverage = 3,
    Average = 4,
    AboveAverage = 5,
    Good = 6,
    VeryGood = 7,
    Excellent = 8,
    Masterwork = 9
}

public static class QualityEnumExtensions
{
    public static string ToHumanizedString(this QualityEnum quality) =>
        Regex.Replace(quality.ToString(), pattern: "(\\B[A-Z])", replacement: " $1");

    public static IPQuality ToItemPropertyEnum(this QualityEnum quality)
    {
        return quality switch
        {
            QualityEnum.Undefined => IPQuality.Unknown,
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
            _ => IPQuality.Unknown
        };
    }
}
