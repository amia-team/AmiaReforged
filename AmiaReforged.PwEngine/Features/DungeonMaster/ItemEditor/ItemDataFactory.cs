using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEdit;

internal static class ItemDataFactory
{
    public static ItemData From(NwItem item)
    {
        return new ItemData(
            item.Name,
            item.Description,
            item.Tag,
            new Dictionary<string, LocalVariableData>()
        );
    }
}
