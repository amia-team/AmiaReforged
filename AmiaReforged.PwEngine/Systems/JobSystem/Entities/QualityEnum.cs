using System.Text.RegularExpressions;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public enum QualityEnum
{
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
    public static string ToHumanizedString(this QualityEnum quality)
    {
        return Regex.Replace(quality.ToString(), "(\\B[A-Z])", " $1");
    }
}