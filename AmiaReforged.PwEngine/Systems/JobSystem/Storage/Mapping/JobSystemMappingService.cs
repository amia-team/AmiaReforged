using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

/// <summary>
///     Maps job system items to their respective entities.
/// </summary>
[ServiceBinding(typeof(JobSystemMappingService))]
public partial class JobSystemMappingService(
    JobItemPropertyHandler propertyHandler,
    QualityPropertyMapper qualityMapper)
    : IMappingService<JobItem, NwItem>
{

    public JobItem MapFrom(NwItem item)
    {
        IPQuality nwnQuality = GetQualityFromItem(item);
        QualityEnum quality = qualityMapper.MapFrom(nwnQuality);

        const string replacement = "";
        string itemName = MyRegex().Replace(item.Name, replacement);

        item.Possessor?.SpeakString($"Processing {item.Name}");

        MaterialEnum material = GetMaterialFromItem(item);

        return JobItemBuilder.CreateJobItem()
            .WithName(itemName)
            .WithDescription(item.Description)
            .WithResRef(item.ResRef)
            .WithBaseValue(propertyHandler.DeriveValue(quality, material))
            .WithMagicModifier(propertyHandler.DeriveMagic(quality, material))
            .WithDurabilityModifier(propertyHandler.DeriveDurability(quality, material))
            .WithType(propertyHandler.DeriveType(item.BaseItem.ItemType))
            .WithQuality(quality)
            .WithMaterial(material)
            .WithIconResRef(item.PortraitResRef)
            .WithSerializedData(item.Serialize()!)
            .Build();
    }

    public NwItem? MapTo(JobItem item)
    {
        NwItem? nwItem = NwItem.Deserialize(item.SerializedData);

        return nwItem;
    }

    private static IPQuality GetQualityFromItem(NwItem item)
    {
        ItemProperty? qualityProperty =
            item.ItemProperties.SingleOrDefault(p => p.Property.PropertyType == ItemPropertyType.Quality);

        IPQuality nwnQuality = qualityProperty == null
            ? IPQuality.Average
            : (IPQuality)qualityProperty.SubType!.RowIndex;
        return nwnQuality;
    }

    private static MaterialEnum GetMaterialFromItem(NwItem item)
    {
        ItemProperty? materialProperty =
            item.ItemProperties.SingleOrDefault(p => p.Property.PropertyType == ItemPropertyType.Material);
        if (materialProperty == null) return MaterialEnum.None;
        
        ItemPropertyCostTableEntry? costTableValue = materialProperty.CostTableValue;
        if (costTableValue == null) return MaterialEnum.None;
        string? value = costTableValue.Name.ToString();
        if (string.IsNullOrEmpty(value)) return MaterialEnum.None;
        MaterialEnum material = (MaterialEnum)costTableValue.RowIndex;
        return material;
    }

    private static ItemProperty FromQuality(QualityEnum itemQuality)
    {
        return itemQuality switch
        {
            QualityEnum.Cut => ItemProperty.Quality(IPQuality.Cut),
            QualityEnum.Raw => ItemProperty.Quality(IPQuality.Raw),
            QualityEnum.VeryPoor => ItemProperty.Quality(IPQuality.VeryPoor),
            QualityEnum.Poor => ItemProperty.Quality(IPQuality.Poor),
            QualityEnum.BelowAverage => ItemProperty.Quality(IPQuality.BelowAverage),
            QualityEnum.Average => ItemProperty.Quality(IPQuality.Average),
            QualityEnum.AboveAverage => ItemProperty.Quality(IPQuality.AboveAverage),
            QualityEnum.Good => ItemProperty.Quality(IPQuality.Good),
            QualityEnum.VeryGood => ItemProperty.Quality(IPQuality.VeryGood),
            QualityEnum.Excellent => ItemProperty.Quality(IPQuality.Excellent),
            QualityEnum.Masterwork => ItemProperty.Quality(IPQuality.Masterwork),
            _ => ItemProperty.Quality(IPQuality.Average)
        };
    }

    [GeneratedRegex(pattern: "<.*?>")]
    private static partial Regex MyRegex();

}