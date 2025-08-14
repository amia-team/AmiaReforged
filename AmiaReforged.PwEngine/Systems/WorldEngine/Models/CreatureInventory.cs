using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public class CreatureInventory(NwCreature? creature) : IItemStorage
{
    public IReadOnlyList<ItemSnapshot> GetItems()
    {
        if (creature == null || !creature.IsValid) return [];

        List<ItemSnapshot> items = [];

        foreach (NwItem item in creature.Inventory.Items)
        {
            LocalVariableInt qualityValue =
                item.GetObjectVariable<LocalVariableInt>(WorldConstants.ItemVariableQuality);

            QualityEnum quality = (QualityEnum)qualityValue.Value;

            if (quality == QualityEnum.None)
            {
                quality = GetItemQualityFromProperty(item);
            }

            MaterialEnum material = MaterialEnum.None;
            if (item.ItemProperties.Any(p => p.Property.PropertyType == ItemPropertyType.Material))
            {
            }

            LocalVariableString maker = item.GetObjectVariable<LocalVariableString>(WorldConstants.ItemVariableMaker);
            Guid? guid = null;
            if (maker.Value != string.Empty)
            {
                if (Guid.TryParse(maker.Value, out Guid id))
                {
                    guid = id;
                }
            }

            ItemType itemType =
                (ItemType)item.GetObjectVariable<LocalVariableInt>(WorldConstants.ItemVariableType).Value;

            items.Add(new ItemSnapshot(itemType, item.Name, item.Tag, quality, material, guid, item, null));
        }

        return items;
    }

    private static QualityEnum GetItemQualityFromProperty(NwItem item)
    {
        QualityEnum quality = QualityEnum.None;
        ItemProperty? qualityProperty =
            item.ItemProperties.FirstOrDefault(p => p.Property.PropertyType == ItemPropertyType.Quality);

        if (qualityProperty == null) return quality;

        string propertyDescription = ItemPropertyHelper.FullPropertyDescription(qualityProperty);

        if (propertyDescription.Contains("Very Poor", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.VeryPoor;
        else if (propertyDescription.Contains("Poor", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.Poor;
        else if (propertyDescription.Contains("Below Average", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.BelowAverage;
        else if (propertyDescription.Contains("Above Average", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.AboveAverage;
        else if (propertyDescription.Contains("Average", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.Average;
        else if (propertyDescription.Contains("Very Good", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.VeryGood;
        else if (propertyDescription.Contains("Good", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.Good;
        else if (propertyDescription.Contains("Excellent", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.Excellent;
        else if (propertyDescription.Contains("Masterwork", StringComparison.OrdinalIgnoreCase))
            quality = QualityEnum.Masterwork;

        return quality;
    }
}
