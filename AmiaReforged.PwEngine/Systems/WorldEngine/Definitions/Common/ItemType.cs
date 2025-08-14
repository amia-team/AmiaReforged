using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

public enum ItemType
{
    Undefined = 0,
    Armor = 1,
    Weapon = 2,
    Gem = 3,
    Geode = 4,
    Ore = 5,
    Ingot = 6,
    Log = 7,
    Fuel = 8, // Coke, charcoal, firewood
    Plank = 9,

    // Unprocessed food and drink ingredients
    FoodIngredient = 10,
    Food = 11,
    Drink = 12,

    // Alchemical ingredients
    PotionIngredient = 13,
    Potion = 14,
    Grain = 15,
    Flour = 16,

    // Academic books, scrolls, ivory, ancient artifacts, etc.
    Scholastic = 17,

    // Unprocessed animal parts
    Pelt = 18,

    // Processed animal parts
    Hide = 19,

    // Jewelry, Paintings, Sculptures, etc.
    Crafts = 20,
    Stone = 21,
    Unknown = 22,
    Ammunition = 23,
    Wand = 24,
    Rod = 25,

    // Game specific item types
    Miscellaneous = 26,
    Key = 27,
}

public static class ItemTypeExtensions
{
    public static ItemType FromNwItem(NwItem item)
    {
        return ItemType.Undefined;
    }
}
