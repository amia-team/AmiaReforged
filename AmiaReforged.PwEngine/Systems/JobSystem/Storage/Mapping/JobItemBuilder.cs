using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

public class JobItemBuilder : IJobItemBuilder
{
    private readonly JobItem _jobItem;

    private JobItemBuilder()
    {
        // Prevent instantiation of this fluent builder.
        _jobItem = new JobItem();
    }
    
    public static IJobItemNamingStep CreateJobItem()
    {
        return new JobItemBuilder();
    }
    
    
    public IJobItemDescriptionStep WithName(string name)
    {
        _jobItem.Name = name;
        return this;
    }

    public IJobItemResRefStep WithDescription(string description)
    {
        _jobItem.Description = description;
        return this;
    }

    public IJobItemBaseValueStep WithResRef(string resRef)
    {
        _jobItem.ResRef = resRef;
        return this;
    }

    public IJobItemMagicStep WithBaseValue(int baseValue)
    {
        _jobItem.BaseValue = baseValue;
        return this;
    }

    public IJobItemDurabilityStep WithMagicModifier(int magicModifier)
    {
        _jobItem.MagicModifier = magicModifier;
        return this;
    }

    public IJobItemTypeStep WithDurabilityModifier(float durabilityModifier)
    {
        _jobItem.DurabilityModifier = durabilityModifier;
        return this;
    }

    public IJobItemQualityStep WithType(ItemType type)
    {
        _jobItem.Type = type;
        return this;
    }

    public IJobItemMaterialStep WithQuality(QualityEnum quality)
    {
        _jobItem.Quality = quality;
        return this;
    }

    public IJobItemCreatorStep WithMaterial(MaterialEnum material)
    {
        _jobItem.Material = material;
        return this;
    }

    public IJobItemIconStep WithCreator(long creator)
    {
        _jobItem.WorldCharacterId = creator;
        return this;
    }

    public IJobItemSerializedStep WithIconResRef(string iconResRef)
    {
        _jobItem.IconResRef = iconResRef;
        return this;
    }

    public IJobItemBuildStep WithSerializedData(byte[] serializedData)
    {
        _jobItem.SerializedData = serializedData;
        return this;
    }

    public JobItem Build()
    {  
        return _jobItem;
    }
}