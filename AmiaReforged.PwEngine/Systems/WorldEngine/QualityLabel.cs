using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public static class QualityLabel
{
    public static string QualityLabelForNode(ResourceType type, IPQuality quality)
    {
        string label = ToItemQualityLabel((int)quality);

        switch (type)
        {
            case ResourceType.Undefined:
                break;
            case ResourceType.Ore:
                label = ToOreQualityLabel((int)quality);
                break;
            case ResourceType.Geode:
                label = ToGemQualityLabel((int)quality);
                break;
            case ResourceType.Boulder:
                label = ToStoneQualityLabel((int)quality);
                break;
            case ResourceType.Tree:
                label = ToTreeQualityLabel((int)quality);
                break;
            case ResourceType.Flora:
                label = ToFloraQualityLabel((int)quality);
                break;
        }

        return label;
    }
    public static string QualityLabelForItem(JobSystemItemType itemType, IPQuality quality)
    {
        string label = ToItemQualityLabel((int)quality);
        switch (itemType)
        {
            case JobSystemItemType.ResourceOre:
                label = ToOreQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourceStone:
                label = ToStoneQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourceLog:
                label = ToLogQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourcePlank:
                label = ToLogQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourceBrick:
                label = ToBrickQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourceIngot:
                label = ToOreQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourceGem:
                label = ToGemQualityLabel((int)quality);
                break;
            case JobSystemItemType.ResourcePlant:
                label = ToFloraQualityLabel((int)quality);
                break;
        }

        return label;
    }

    public static string ToItemQualityLabel(int quality) => quality switch
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

    private static string ToTreeQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Withered",
        NWScript.IP_CONST_QUALITY_POOR => "Stunted",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Struggling",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Healthy",
        NWScript.IP_CONST_QUALITY_GOOD => "Thriving",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Flourishing",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Vigorous",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Ancient",
        _ => ""
    };

    private static string ToLogQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Irregular",
        NWScript.IP_CONST_QUALITY_POOR => "Knotted",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Uneven",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Decent",
        NWScript.IP_CONST_QUALITY_GOOD => "Quality",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Premium",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Select",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Exquisite",
        _ => ""
    };

    public static string ToPlankQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Warped",
        NWScript.IP_CONST_QUALITY_POOR => "Rough",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Common",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Standard",
        NWScript.IP_CONST_QUALITY_GOOD => "Fine",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Premium",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Select",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Exquisite",
        _ => ""
    };

    private static string ToOreQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Impure",
        NWScript.IP_CONST_QUALITY_POOR => "Common",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Fair",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Rich",
        NWScript.IP_CONST_QUALITY_GOOD => "Pure",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Premium",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Pristine",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Legendary",
        _ => ""
    };

    private static string ToBrickQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Crumbling",
        NWScript.IP_CONST_QUALITY_POOR => "Cracked",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Rough",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Solid",
        NWScript.IP_CONST_QUALITY_GOOD => "Fine",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Premium",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Pristine",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Flawless",
        _ => ""
    };

    private static string ToGemQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Flawed",
        NWScript.IP_CONST_QUALITY_POOR => "Chipped",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Rough",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Clear",
        NWScript.IP_CONST_QUALITY_GOOD => "Fine",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Flawless",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Perfect",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Exquisite",
        _ => ""
    };

    private static string ToStoneQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Crumbling",
        NWScript.IP_CONST_QUALITY_POOR => "Fragmented",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Rough",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Solid",
        NWScript.IP_CONST_QUALITY_GOOD => "Dense",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Premium",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Pristine",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Flawless",
        _ => ""
    };

    private static string ToFloraQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Wilted",
        NWScript.IP_CONST_QUALITY_POOR => "Damaged",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Common",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Fresh",
        NWScript.IP_CONST_QUALITY_GOOD => "Prime",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Choice",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Pristine",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Perfect",
        _ => ""
    };

    public static string ToMeatQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Sinewy",
        NWScript.IP_CONST_QUALITY_POOR => "Tough",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Common",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Fresh",
        NWScript.IP_CONST_QUALITY_GOOD => "Prime",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Choice",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Select",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Premium",
        _ => ""
    };

    public static string ToAnimalQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Sickly",
        NWScript.IP_CONST_QUALITY_POOR => "Weak",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Common",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Healthy",
        NWScript.IP_CONST_QUALITY_GOOD => "Robust",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Hardy",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Superior",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Majestic",
        _ => ""
    };

    public static string ToHideQualityLabel(int quality) => quality switch
    {
        NWScript.IP_CONST_QUALITY_VERY_POOR => "Damaged",
        NWScript.IP_CONST_QUALITY_POOR => "Flawed",
        NWScript.IP_CONST_QUALITY_BELOW_AVERAGE => "Common",
        NWScript.IP_CONST_QUALITY_ABOVE_AVERAGE => "Fine",
        NWScript.IP_CONST_QUALITY_GOOD => "Select",
        NWScript.IP_CONST_QUALITY_VERY_GOOD => "Premium",
        NWScript.IP_CONST_QUALITY_EXCELLENT => "Flawless",
        NWScript.IP_CONST_QUALITY_MASTERWORK => "Perfect",
        _ => ""
    };
}
