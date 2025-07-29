using AmiaReforged.PwEngine.Systems.Crafting;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.JobSystem;

[ServiceBinding(typeof(BaseItemTypeMapper))]
public class BaseItemTypeMapper : IMapAtoB<ItemType, NwItem>
{
    public ItemType MapFrom(NwItem b)
    {
        ItemType itemType = ItemType.Unknown;

        if (ItemTypeConstants.Melee2HWeapons().Contains(NWScript.GetBaseItemType(b)) ||
            ItemTypeConstants.MeleeWeapons().Contains(NWScript.GetBaseItemType(b)) ||
            ItemTypeConstants.RangedWeapons().Contains(NWScript.GetBaseItemType(b)) ||
            ItemTypeConstants.ThrownWeapons().Contains(NWScript.GetBaseItemType(b)))
        {
            itemType = ItemType.Weapon;
        }

        if (b.BaseItem.ItemType == BaseItemType.Armor)
        {
            itemType = ItemType.Armor;
        }

        if (b.BaseItem.ItemType == BaseItemType.LargeShield ||
            b.BaseItem.ItemType == BaseItemType.SmallShield ||
            b.BaseItem.ItemType == BaseItemType.TowerShield)
        {
            itemType = ItemType.Armor;
        }

        
        if (b.BaseItem.ItemType == BaseItemType.Arrow ||
            b.BaseItem.ItemType == BaseItemType.Bolt ||
            b.BaseItem.ItemType == BaseItemType.Bullet ||
            b.BaseItem.ItemType == BaseItemType.Shuriken ||
            b.BaseItem.ItemType == BaseItemType.Dart || b.BaseItem.ItemType == BaseItemType.ThrowingAxe
           )
        {
            itemType = ItemType.Ammunition;
        }
        
        if(b.BaseItem.ItemType == BaseItemType.Amulet || 
           b.BaseItem.ItemType == BaseItemType.Belt ||
           b.BaseItem.ItemType == BaseItemType.Cloak ||
           b.BaseItem.ItemType == BaseItemType.Bracer ||
           b.BaseItem.ItemType == BaseItemType.Gloves ||
           b.BaseItem.ItemType == BaseItemType.Ring)
        {
            itemType = ItemType.Crafts;
        }

        return itemType;
    }
}