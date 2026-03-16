using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaterialEnum
{
    None = -1,
    Unknown = 0,

    // === Metals ===
    [MaterialCategory(MaterialCategory.Metal)] Adamantine = 1,
    [MaterialCategory(MaterialCategory.Metal)] Brass = 2,
    [MaterialCategory(MaterialCategory.Metal)] Bronze = 3,
    [MaterialCategory(MaterialCategory.Metal)] Carbon = 4,
    [MaterialCategory(MaterialCategory.Metal)] ColdIron = 5,
    [MaterialCategory(MaterialCategory.Metal)] Copper = 6,
    [MaterialCategory(MaterialCategory.Metal)] Darksteel = 7,
    [MaterialCategory(MaterialCategory.Metal)] Gold = 8,
    [MaterialCategory(MaterialCategory.Metal)] Iron = 9,
    [MaterialCategory(MaterialCategory.Metal)] Lead = 10,
    [MaterialCategory(MaterialCategory.Metal)] Mithral = 11,
    [MaterialCategory(MaterialCategory.Metal)] Platinum = 12,
    [MaterialCategory(MaterialCategory.Metal)] Silver = 13,
    [MaterialCategory(MaterialCategory.Metal)] SilverAlchemical = 14,
    [MaterialCategory(MaterialCategory.Metal)] Steel = 15,

    // === Creature (Bone, Hides, Leather, Scale) ===
    [MaterialCategory(MaterialCategory.Creature)] Bone = 16,
    [MaterialCategory(MaterialCategory.Creature)] Hide = 17,
    [MaterialCategory(MaterialCategory.Creature)] HideSalamander = 18,
    [MaterialCategory(MaterialCategory.Creature)] HideUmberHulk = 19,
    [MaterialCategory(MaterialCategory.Creature)] HideWyvern = 20,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonBlack = 21,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonBlue = 22,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonBrass = 23,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonBronze = 24,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonCopper = 25,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonGold = 26,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonGreen = 27,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonRed = 28,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonSilver = 29,
    [MaterialCategory(MaterialCategory.Creature)] HideDragonWhite = 30,
    [MaterialCategory(MaterialCategory.Creature)] Leather = 31,
    [MaterialCategory(MaterialCategory.Creature)] Scale = 32,

    // === Plant (Textiles) ===
    [MaterialCategory(MaterialCategory.Plant)] Cloth = 33,
    [MaterialCategory(MaterialCategory.Plant)] Cotton = 34,
    [MaterialCategory(MaterialCategory.Plant)] Silk = 35,
    [MaterialCategory(MaterialCategory.Plant)] Wool = 36,

    // === Woods ===
    [MaterialCategory(MaterialCategory.Wood)] Wood = 37,
    [MaterialCategory(MaterialCategory.Wood)] WoodIronwood = 38,
    [MaterialCategory(MaterialCategory.Wood)] WoodDuskwood = 39,
    [MaterialCategory(MaterialCategory.Wood)] WoodDarkwoodZalantar = 40,
    [MaterialCategory(MaterialCategory.Wood)] WoodAsh = 41,
    [MaterialCategory(MaterialCategory.Wood)] WoodYew = 42,
    [MaterialCategory(MaterialCategory.Wood)] WoodOak = 43,
    [MaterialCategory(MaterialCategory.Wood)] WoodPine = 44,
    [MaterialCategory(MaterialCategory.Wood)] WoodCedar = 45,

    // === Elementals ===
    [MaterialCategory(MaterialCategory.Elemental)] Elemental = 46,
    [MaterialCategory(MaterialCategory.Elemental)] ElementalAir = 47,
    [MaterialCategory(MaterialCategory.Elemental)] ElementalEarth = 48,
    [MaterialCategory(MaterialCategory.Elemental)] ElementalFire = 49,
    [MaterialCategory(MaterialCategory.Elemental)] ElementalWater = 50,

    // === Gems ===
    [MaterialCategory(MaterialCategory.Gem)] Gem = 51,
    [MaterialCategory(MaterialCategory.Gem)] GemAlexandrite = 52,
    [MaterialCategory(MaterialCategory.Gem)] GemAmethyst = 53,
    [MaterialCategory(MaterialCategory.Gem)] GemAventurine = 54,
    [MaterialCategory(MaterialCategory.Gem)] GemBeljuril = 55,
    [MaterialCategory(MaterialCategory.Gem)] GemBloodstone = 56,
    [MaterialCategory(MaterialCategory.Gem)] GemBlueDiamond = 57,
    [MaterialCategory(MaterialCategory.Gem)] GemCanaryDiamond = 58,
    [MaterialCategory(MaterialCategory.Gem)] GemDiamond = 59,
    [MaterialCategory(MaterialCategory.Gem)] GemEmerald = 60,
    [MaterialCategory(MaterialCategory.Gem)] GemFireAgate = 61,
    [MaterialCategory(MaterialCategory.Gem)] GemFireOpal = 62,
    [MaterialCategory(MaterialCategory.Gem)] GemFluorspar = 63,
    [MaterialCategory(MaterialCategory.Gem)] GemGarnet = 64,
    [MaterialCategory(MaterialCategory.Gem)] GemGreenstone = 65,
    [MaterialCategory(MaterialCategory.Gem)] GemJacinth = 66,
    [MaterialCategory(MaterialCategory.Gem)] GemKingsTear = 67,
    [MaterialCategory(MaterialCategory.Gem)] GemMalachite = 68,
    [MaterialCategory(MaterialCategory.Gem)] GemObsidian = 69,
    [MaterialCategory(MaterialCategory.Gem)] GemPhenalope = 70,
    [MaterialCategory(MaterialCategory.Gem)] GemRogueStone = 71,
    [MaterialCategory(MaterialCategory.Gem)] GemRuby = 72,
    [MaterialCategory(MaterialCategory.Gem)] GemSapphire = 73,
    [MaterialCategory(MaterialCategory.Gem)] GemStarSapphire = 74,
    [MaterialCategory(MaterialCategory.Gem)] GemTopaz = 75,
    [MaterialCategory(MaterialCategory.Gem)] GemCrystalDeep = 76,
    [MaterialCategory(MaterialCategory.Gem)] GemCrystalMundane = 77,

    // === More Woods ===
    [MaterialCategory(MaterialCategory.Wood)] WoodPhandar = 78,
    [MaterialCategory(MaterialCategory.Wood)] WoodShadowtop = 79,
    [MaterialCategory(MaterialCategory.Wood)] WoodZhurkwood = 80,

    // === More Metals ===
    [MaterialCategory(MaterialCategory.Metal)] Adamant = 81,
}
