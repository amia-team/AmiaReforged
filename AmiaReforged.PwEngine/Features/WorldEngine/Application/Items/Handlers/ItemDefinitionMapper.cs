using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

// Mapper no longer needed after consolidation to ItemBlueprint; file deprecated.
// Keeping stub for now to avoid build breaks if referenced; returns same object.

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

internal static class ItemDefinitionMapper
{
    public static ItemBlueprint? Map(ItemBlueprint? source) => source;
}
