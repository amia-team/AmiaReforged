using AmiaReforged.PwEngine.Systems.Crafting;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

[ServiceBinding(typeof(ItemTypeResRefMapper))]
public class ItemTypeResRefMapper : IMapAtoB<ItemType, string>
{
    public ItemType MapFrom(string resRef)
    {
        ItemType itemType = ItemType.Unknown;

        // If it matches the pattern "js_bla_*in", then it's an ingot...
        if (resRef.StartsWith("js_bla_") && resRef.EndsWith("in"))
        {
            itemType = ItemType.Ingot;
        }

        if (resRef.StartsWith("js_met") && resRef.EndsWith('o'))
        {
            itemType = ItemType.Ore;
        }

        if (resRef == "js_bla_arfp")
        {
            itemType = ItemType.Armor;
        }

        if (resRef.StartsWith("js_sch"))
        {
            itemType = ItemType.Scholastic;
        }

        return itemType;
    }
}