using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

internal static class ItemDefinitionMapper
{
    public static PublicItemDefinition? Map(InternalItemDefinition? source)
    {
        if (source == null) return null;

        ItemCategory category = source.JobSystemType switch
        {
            JobSystemItemType.ResourceOre => ItemCategory.Resource,
            JobSystemItemType.ResourceStone => ItemCategory.Resource,
            JobSystemItemType.ResourceLog => ItemCategory.Resource,
            JobSystemItemType.ResourcePlank => ItemCategory.Resource,
            JobSystemItemType.ResourceBrick => ItemCategory.Resource,
            JobSystemItemType.ResourceIngot => ItemCategory.Resource,
            JobSystemItemType.ResourceGem => ItemCategory.Resource,
            JobSystemItemType.ResourcePlant => ItemCategory.Resource,
            _ => ItemCategory.Miscellaneous
        };

        Dictionary<string, object> custom = new();
        // Future enrichment: materials, appearance, weight.

        return new PublicItemDefinition(
            source.ResRef,
            source.Name,
            source.Description,
            category,
            source.BaseValue,
            custom);
    }
}
