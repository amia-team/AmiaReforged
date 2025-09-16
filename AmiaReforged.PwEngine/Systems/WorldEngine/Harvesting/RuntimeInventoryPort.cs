using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public class RuntimeInventoryPort(NwCreature creature) : IInventoryPort
{
    private const int MiscSmall2 = 119;
    private const int MiscSmall3 = 120;

    public void AddItem(ItemDto item)
    {
        if (creature.Location is null) return;

        NwItem? gameItem = NwItem.Create(item.BaseDefinition.ResRef, creature.Location);

        if (gameItem is null) return;

        string qualityLabel = QualityLabel.ToQualityLabel((int)item.Quality);
        gameItem.Name = $"{item.BaseDefinition.Name} ({qualityLabel})";

        ItemProperty quality = ItemProperty.Quality(item.Quality);
        gameItem.AddItemProperty(quality, EffectDuration.Permanent);
        gameItem.Description = item.BaseDefinition.Description;

        if (item.BaseDefinition.Appearance.ModelType
            is NWScript.BASE_ITEM_MISCSMALL
            or MiscSmall2
            or MiscSmall3)
        {
            gameItem.Appearance.SetSimpleModel((ushort)(item.BaseDefinition.Appearance.SimpleModelNumber ?? 1));
        }

        creature.AcquireItem(gameItem);
    }

    public List<ItemSnapshot> GetInventory()
    {
        return [];
    }

    public Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment()
    {
        return new Dictionary<EquipmentSlots, ItemSnapshot?>()
        {
            { EquipmentSlots.Head, ToItemSnapshot(creature.GetItemInSlot(InventorySlot.Head)) }
        };
    }

    private ItemSnapshot? ToItemSnapshot(NwItem? equipped)
    {
        if (equipped is null) return null;

        int storedQuality = NWScript.GetLocalInt(equipped, WorldConstants.ItemVariableQuality);
        int storedToolType = NWScript.GetLocalInt(equipped, WorldConstants.ToolTypeVariable);

        IPQuality quality = storedQuality == 0 ? IPQuality.Unknown : (IPQuality)storedQuality;
        return new ItemSnapshot(equipped.Tag,
            equipped.Name,
            equipped.Description,
            quality,
            [],
            (JobSystemItemType)storedToolType,
            NWScript.GetBaseItemType(equipped),
            equipped.Serialize());
    }

    public static RuntimeInventoryPort For(NwCreature creature)
    {
        return new RuntimeInventoryPort(creature);
    }
}

public static class QualityLabel
{
    public static string ToQualityLabel(int quality) => quality switch
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
}
