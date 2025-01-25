using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CraftingBudgetService))]
public class CraftingBudgetService
{
    public int MythalBudgetForNwItem(NwItem item)
    {
        int baseItemType = NWScript.GetBaseItemType(item);
        int casterWeapon = NWScript.GetLocalInt(item, "CASTER_WEAPON");
        bool isTwoHanded = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);

        int trueBase = baseItemType;
        if (casterWeapon == 1)
        {
            trueBase = isTwoHanded ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;
        }

        return MythalBudgetFor(trueBase);
    }

    public int MythalBudgetFor(int baseItemType)
    {
        return baseItemType switch
        {
            // Equippables
            NWScript.BASE_ITEM_AMULET => 4,
            NWScript.BASE_ITEM_ARMOR => 8,
            NWScript.BASE_ITEM_TOWERSHIELD => 8,
            NWScript.BASE_ITEM_SMALLSHIELD => 8,
            NWScript.BASE_ITEM_LARGESHIELD => 8,
            NWScript.BASE_ITEM_GLOVES => 8,
            NWScript.BASE_ITEM_BELT => 8,
            NWScript.BASE_ITEM_BRACER => 8,
            NWScript.BASE_ITEM_CLOAK => 8,
            NWScript.BASE_ITEM_BOOTS => 8,
            NWScript.BASE_ITEM_HELMET => 6,
            NWScript.BASE_ITEM_RING => 6,
            //2H Melee Weapons are 8
            NWScript.BASE_ITEM_HEAVYFLAIL => 8,
            NWScript.BASE_ITEM_TWOBLADEDSWORD => 8,
            NWScript.BASE_ITEM_GREATAXE => 8,
            NWScript.BASE_ITEM_GREATSWORD => 8,
            NWScript.BASE_ITEM_HALBERD => 8,
            NWScript.BASE_ITEM_DIREMACE => 8,
            NWScript.BASE_ITEM_DOUBLEAXE => 8,
            NWScript.BASE_ITEM_SCYTHE => 8,
            NWScript.BASE_ITEM_SHORTSPEAR => 8,
            NWScript.BASE_ITEM_QUARTERSTAFF => 8,
            // 1h weapons are 6
            NWScript.BASE_ITEM_MAGICSTAFF => 6,
            NWScript.BASE_ITEM_BASTARDSWORD => 6,
            NWScript.BASE_ITEM_BATTLEAXE => 6,
            NWScript.BASE_ITEM_CLUB => 6,
            NWScript.BASE_ITEM_DAGGER => 6,
            NWScript.BASE_ITEM_DWARVENWARAXE => 6,
            NWScript.BASE_ITEM_HANDAXE => 6,
            NWScript.BASE_ITEM_KAMA => 6,
            NWScript.BASE_ITEM_KATANA => 6,
            NWScript.BASE_ITEM_KUKRI => 6,
            NWScript.BASE_ITEM_LIGHTFLAIL => 6,
            NWScript.BASE_ITEM_LIGHTHAMMER => 6,
            NWScript.BASE_ITEM_LIGHTMACE => 6,
            NWScript.BASE_ITEM_LONGSWORD => 6,
            NWScript.BASE_ITEM_MORNINGSTAR => 6,
            NWScript.BASE_ITEM_RAPIER => 6,
            NWScript.BASE_ITEM_SCIMITAR => 6,
            NWScript.BASE_ITEM_SHORTSWORD => 6,
            NWScript.BASE_ITEM_SICKLE => 6,
            NWScript.BASE_ITEM_TRIDENT => 6,
            NWScript.BASE_ITEM_WARHAMMER => 6,
            NWScript.BASE_ITEM_WHIP => 6,
            //Thrown Weapons are 5
            NWScript.BASE_ITEM_SHURIKEN => 5,
            NWScript.BASE_ITEM_DART => 5,
            NWScript.BASE_ITEM_THROWINGAXE => 5,
            // Ranged Weapons are 6
            NWScript.BASE_ITEM_LONGBOW => 6,
            NWScript.BASE_ITEM_SHORTBOW => 6,
            NWScript.BASE_ITEM_LIGHTCROSSBOW => 6,
            NWScript.BASE_ITEM_HEAVYCROSSBOW => 6,
            NWScript.BASE_ITEM_SLING => 6,
            // Ammo is 4
            NWScript.BASE_ITEM_ARROW => 4,
            NWScript.BASE_ITEM_BOLT => 4,
            NWScript.BASE_ITEM_BULLET => 4,
            // Special case where there isn't actually a set base item type
            CraftingPropertyData.CasterWeapon1H => 12,
            CraftingPropertyData.CasterWeapon2H => 20,
            _ => 0
        };
    }
}