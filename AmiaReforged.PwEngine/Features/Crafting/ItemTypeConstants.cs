using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting;

/// <summary>
///     A class containing collections of items grouped to specific item categories to simplify querying which categories items belong to.
/// </summary>
public static class ItemTypeConstants
{
    public const string CasterWeaponVar = "CASTER_WEAPON";

    /// <summary>
    /// The resref of the Artificer Mythal Tube item that stores mythals virtually.
    /// </summary>
    public const string MythalTubeResRef = "js_arca_mytu";

    /// <summary>
    /// The local variable name used to store the item count on storage containers.
    /// </summary>
    public const string StorageItemCountVar = "ItemCount";

    /// <summary>
    /// The local variable name used to store the stored item resref on storage containers.
    /// </summary>
    public const string StoredItemVar = "StoredItem";

    public static List<int> MeleeWeapons() =>
    [
        NWScript.BASE_ITEM_BASTARDSWORD,
        NWScript.BASE_ITEM_BATTLEAXE,
        NWScript.BASE_ITEM_CLUB,
        NWScript.BASE_ITEM_DAGGER,
        NWScript.BASE_ITEM_DWARVENWARAXE,
        NWScript.BASE_ITEM_HANDAXE,
        NWScript.BASE_ITEM_KAMA,
        NWScript.BASE_ITEM_KATANA,
        NWScript.BASE_ITEM_KUKRI,
        NWScript.BASE_ITEM_LIGHTFLAIL,
        NWScript.BASE_ITEM_LIGHTHAMMER,
        NWScript.BASE_ITEM_LIGHTMACE,
        NWScript.BASE_ITEM_LONGSWORD,
        NWScript.BASE_ITEM_MORNINGSTAR,
        NWScript.BASE_ITEM_RAPIER,
        NWScript.BASE_ITEM_SCIMITAR,
        NWScript.BASE_ITEM_SHORTSWORD,
        NWScript.BASE_ITEM_SICKLE,
        NWScript.BASE_ITEM_TRIDENT,
        NWScript.BASE_ITEM_WARHAMMER,
        NWScript.BASE_ITEM_WHIP,
        NWScript.BASE_ITEM_MAGICSTAFF
    ];

    public static List<int> Melee2HWeapons() =>
    [
        NWScript.BASE_ITEM_HEAVYFLAIL,
        NWScript.BASE_ITEM_TWOBLADEDSWORD,
        NWScript.BASE_ITEM_GREATAXE,
        NWScript.BASE_ITEM_GREATSWORD,
        NWScript.BASE_ITEM_HALBERD,
        NWScript.BASE_ITEM_DIREMACE,
        NWScript.BASE_ITEM_DOUBLEAXE,
        NWScript.BASE_ITEM_SCYTHE,
        NWScript.BASE_ITEM_SHORTSPEAR,
        NWScript.BASE_ITEM_QUARTERSTAFF
    ];

    public static List<int> ThrownWeapons() =>
    [
        NWScript.BASE_ITEM_SHURIKEN,
        NWScript.BASE_ITEM_DART,
        NWScript.BASE_ITEM_THROWINGAXE
    ];

    public static List<int> Ammo() =>
    [
        NWScript.BASE_ITEM_ARROW,
        NWScript.BASE_ITEM_BOLT,
        NWScript.BASE_ITEM_BULLET
    ];

    public static List<int> RangedWeapons() =>
    [
        NWScript.BASE_ITEM_LONGBOW,
        NWScript.BASE_ITEM_SHORTBOW,
        NWScript.BASE_ITEM_LIGHTCROSSBOW,
        NWScript.BASE_ITEM_HEAVYCROSSBOW,
        NWScript.BASE_ITEM_SLING
    ];

    /// <summary>
    ///     All equippable items that can be used in crafting recipes. Certain special items were left commented out for
    ///     documentation purposes.
    /// </summary>
    /// <returns></returns>
    public static List<int> EquippableItems() =>
    [
        NWScript.BASE_ITEM_ARMOR,
        NWScript.BASE_ITEM_BELT,
        NWScript.BASE_ITEM_BOOTS,
        NWScript.BASE_ITEM_BRACER,
        NWScript.BASE_ITEM_CLOAK,
        // NWScript.BASE_ITEM_GLOVES, SPECIAL CASE, HANDLED SEPARATELY
        NWScript.BASE_ITEM_HELMET,
        NWScript.BASE_ITEM_LARGESHIELD,
        // NWScript.BASE_ITEM_MAGICSTAFF, SPECIAL CASE, HANDLED SEPARATELY
        NWScript.BASE_ITEM_RING,
        NWScript.BASE_ITEM_SMALLSHIELD,
        NWScript.BASE_ITEM_TOWERSHIELD
    ];
}
