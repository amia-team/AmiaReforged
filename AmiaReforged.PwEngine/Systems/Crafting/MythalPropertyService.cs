using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

// [ServiceBinding(typeof(MythalPropertyService))]
public class MythalPropertyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<CraftingTier, List<MythalProperty>> _properties = new();

    public MythalPropertyService()
    {

    }


}

public static class ItemTypeConstants
{
    public static List<int> MeleeWeapons()
    {
        return new List<int>
        {
            NWScript.BASE_ITEM_BASTARDSWORD,
            NWScript.BASE_ITEM_BATTLEAXE,
            NWScript.BASE_ITEM_CLUB,
            NWScript.BASE_ITEM_DAGGER,
            NWScript.BASE_ITEM_DART,
            NWScript.BASE_ITEM_DIREMACE,
            NWScript.BASE_ITEM_DOUBLEAXE,
            NWScript.BASE_ITEM_DWARVENWARAXE,
            NWScript.BASE_ITEM_GLOVES,
            NWScript.BASE_ITEM_GREATAXE,
            NWScript.BASE_ITEM_GREATSWORD,
            NWScript.BASE_ITEM_HALBERD,
            NWScript.BASE_ITEM_HANDAXE,
            NWScript.BASE_ITEM_HEAVYFLAIL,
            NWScript.BASE_ITEM_KAMA,
            NWScript.BASE_ITEM_KATANA,
            NWScript.BASE_ITEM_KUKRI,
            NWScript.BASE_ITEM_LIGHTFLAIL,
            NWScript.BASE_ITEM_LIGHTHAMMER,
            NWScript.BASE_ITEM_LIGHTMACE,
            NWScript.BASE_ITEM_LONGSWORD,
            NWScript.BASE_ITEM_MORNINGSTAR,
            NWScript.BASE_ITEM_QUARTERSTAFF,
            NWScript.BASE_ITEM_RAPIER,
            NWScript.BASE_ITEM_SCIMITAR,
            NWScript.BASE_ITEM_SCYTHE,
            NWScript.BASE_ITEM_SHORTSPEAR,
            NWScript.BASE_ITEM_SHORTSWORD,
            NWScript.BASE_ITEM_SHURIKEN,
            NWScript.BASE_ITEM_SICKLE,
            NWScript.BASE_ITEM_THROWINGAXE,
            NWScript.BASE_ITEM_TRIDENT,
            NWScript.BASE_ITEM_TWOBLADEDSWORD,
            NWScript.BASE_ITEM_WARHAMMER,
            NWScript.BASE_ITEM_WHIP
        };
    }

    public static List<int> EquippableItems()
    {
        return new List<int>()
        {
            NWScript.BASE_ITEM_AMULET,
            NWScript.BASE_ITEM_ARMOR,
            NWScript.BASE_ITEM_BELT,
            NWScript.BASE_ITEM_BOOTS,
            NWScript.BASE_ITEM_BRACER,
            NWScript.BASE_ITEM_CLOAK,
            NWScript.BASE_ITEM_GLOVES,
            NWScript.BASE_ITEM_HELMET,
            NWScript.BASE_ITEM_LARGESHIELD,
            NWScript.BASE_ITEM_MAGICSTAFF,
            NWScript.BASE_ITEM_RING,
            NWScript.BASE_ITEM_SMALLSHIELD,
            NWScript.BASE_ITEM_TOWERSHIELD
        };
    }
}