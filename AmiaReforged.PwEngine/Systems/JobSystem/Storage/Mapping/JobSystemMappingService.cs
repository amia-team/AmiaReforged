using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

/// <summary>
/// Maps job system items to their respective entities.
/// </summary>
[ServiceBinding(typeof(JobSystemMappingService))]
public class JobSystemMappingService : IMappingService<JobItem, NwItem>
{
    private readonly PwEngineContext _context;
    private readonly JobItemPropertyHandler _propertyHandler;
    private readonly QualityPropertyMapper _qualityMapper;

    public JobSystemMappingService(PwEngineContext context, JobItemPropertyHandler propertyHandler,
        QualityPropertyMapper qualityMapper)
    {
        _context = context;
        _propertyHandler = propertyHandler;
        _qualityMapper = qualityMapper;
    }

    public JobItem MapFrom(NwItem item)
    {
        IPQuality nwnQuality = GetQualityFromitem(item);
        QualityEnum quality = _qualityMapper.MapFrom(nwnQuality);

        MaterialEnum material = GetMaterialFromItem(item);
        
        return JobItemBuilder.CreateJobItem()
            .WithName(item.Name)
            .WithDescription(item.Description)
            .WithResRef(item.ResRef)
            .WithBaseValue(_propertyHandler.DeriveValue(quality, material))
            .WithMagicModifier(_propertyHandler.DeriveMagic(quality, material))
            .WithDurabilityModifier(_propertyHandler.DeriveDurability(quality, material))
            .WithType(_propertyHandler.DeriveType(item.BaseItem.ItemType))
            .WithQuality(quality)
            .WithMaterial(material)
            .WithCreator(long.Parse(NWScript.GetLocalString(item, "MadeBy")))
            .WithIconResRef(item.PortraitResRef)
            .WithSerializedData(item.Serialize()!)
            .Build();
    }

    private static IPQuality GetQualityFromitem(NwItem item)
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
        MaterialEnum material = materialProperty == null
            ? MaterialEnum.None
            : (MaterialEnum)materialProperty.SubType!.RowIndex;
        return material;
    }
    
    public NwItem? MapTo(JobItem item)
    {
        NwItem? nwItem = NwItem.Deserialize(item.SerializedData);

        return nwItem;
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
}