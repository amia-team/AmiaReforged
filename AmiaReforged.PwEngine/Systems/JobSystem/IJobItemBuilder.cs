using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.JobSystem;

public interface IJobItemNamingStep
{
    IJobItemDescriptionStep WithName(string name);
}

public interface IJobItemDescriptionStep
{
    IJobItemResRefStep WithDescription(string description);
}

public interface IJobItemResRefStep
{
    IJobItemBaseValueStep WithResRef(string resRef);
}

public interface IJobItemBaseValueStep
{
    IJobItemMagicStep WithBaseValue(int baseValue);
}

public interface IJobItemMagicStep
{
    IJobItemDurabilityStep WithMagicModifier(int magicModifier);
}

public interface IJobItemDurabilityStep
{
    IJobItemTypeStep WithDurabilityModifier(float durabilityModifier);
}

public interface IJobItemTypeStep
{
    IJobItemQualityStep WithType(ItemType type);
}

public interface IJobItemQualityStep
{
    IJobItemMaterialStep WithQuality(QualityEnum quality);
}

public interface IJobItemMaterialStep
{
    IJobItemCreatorStep WithMaterial(MaterialEnum material);
}

public interface IJobItemCreatorStep
{
    IJobItemIconStep WithCreator(long creator);
}

public interface IJobItemIconStep
{
    IJobItemSerializedStep WithIconResRef(string iconResRef);
}

public interface IJobItemSerializedStep
{
    IJobItemBuildStep WithSerializedData(byte[] serializedData);
}

public interface IJobItemBuildStep
{
    JobItem Build();
}

public interface IJobItemBuilder : IJobItemNamingStep, IJobItemDescriptionStep, IJobItemResRefStep,
    IJobItemBaseValueStep, IJobItemMagicStep, IJobItemDurabilityStep, IJobItemTypeStep, IJobItemQualityStep,
    IJobItemMaterialStep, IJobItemCreatorStep, IJobItemIconStep, IJobItemSerializedStep, IJobItemBuildStep
{
}