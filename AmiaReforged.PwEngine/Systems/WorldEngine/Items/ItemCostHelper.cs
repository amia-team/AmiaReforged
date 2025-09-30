using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public static class ItemCostHelper
{
    public static float GetCostMultiplier(IPQuality quality) => quality switch
    {
        IPQuality.Average => 0.05f,
        IPQuality.AboveAverage => 0.1f,
        IPQuality.Good => 0.15f,
        IPQuality.VeryGood => 0.2f,
        IPQuality.Excellent => 0.25f,
        IPQuality.Masterwork => 0.5f,
        _ => 0f
    };

    public static int GetAddedCost(ItemDto item, MaterialEnum[] baseDefinitionMaterials)
    {
        return baseDefinitionMaterials.Sum(m => (int)(item.BaseDefinition.BaseValue * GetMaterialCostMultiplier(m)));
    }

    private static float GetMaterialCostMultiplier(MaterialEnum material)
    {
        return material switch
        {
            MaterialEnum.None => 0,
            MaterialEnum.Unknown => 0,
            MaterialEnum.Adamantine => 1.0f,
            MaterialEnum.Brass => 0.4f,
            MaterialEnum.Bronze => 0.2f,
            MaterialEnum.Carbon => 0.3f,
            MaterialEnum.ColdIron => 0.45f,
            MaterialEnum.Copper => 0.1f,
            MaterialEnum.Darksteel => 0.5f,
            MaterialEnum.Gold => 0.9f,
            MaterialEnum.Iron => 0.05f,
            MaterialEnum.Lead => 0.03f,
            MaterialEnum.Mithral => 0.95f,
            MaterialEnum.Platinum => 1.1f,
            MaterialEnum.Silver => 0.8f,
            MaterialEnum.SilverAlchemical => 0.75f,
            MaterialEnum.Steel => 0.4f,
            MaterialEnum.Bone => 0f,
            MaterialEnum.Hide => 0f,
            MaterialEnum.HideSalamander => 0f,
            MaterialEnum.HideUmberHulk => 0.4f,
            MaterialEnum.HideWyvern => 0.5f,
            MaterialEnum.HideDragonBlack => 1.1f,
            MaterialEnum.HideDragonBlue => 1.1f,
            MaterialEnum.HideDragonBrass => 1.1f,
            MaterialEnum.HideDragonBronze => 1.1f,
            MaterialEnum.HideDragonCopper => 1.1f,
            MaterialEnum.HideDragonGold => 1.1f,
            MaterialEnum.HideDragonGreen => 1.1f,
            MaterialEnum.HideDragonRed => 1.1f,
            MaterialEnum.HideDragonSilver => 1.1f,
            MaterialEnum.HideDragonWhite => 1.1f,
            MaterialEnum.Leather => 0f,
            MaterialEnum.Scale => 0f,
            MaterialEnum.Cloth => 0f,
            MaterialEnum.Cotton => 0.3f,
            MaterialEnum.Silk => 0.4f,
            MaterialEnum.Wool => 0.3f,
            MaterialEnum.Wood => 0f,
            MaterialEnum.WoodIronwood => 0.25f,
            MaterialEnum.WoodDuskwood => 0.3f,
            MaterialEnum.WoodDarkwoodZalantar => 0.4f,
            MaterialEnum.WoodAsh => 0.25f,
            MaterialEnum.WoodYew => 0.25f,
            MaterialEnum.WoodOak => 0.15f,
            MaterialEnum.WoodPine => 0.05f,
            MaterialEnum.WoodCedar => 0.1f,
            MaterialEnum.Elemental => 0f,
            MaterialEnum.ElementalAir => 0.1f,
            MaterialEnum.ElementalEarth => 0.1f,
            MaterialEnum.ElementalFire => 0.1f,
            MaterialEnum.ElementalWater => 0.1f,
            MaterialEnum.Gem => 0f,
            MaterialEnum.GemAlexandrite => 0.2f,
            MaterialEnum.GemAmethyst => 0.5f,
            MaterialEnum.GemAventurine => 0.3f,
            MaterialEnum.GemBeljuril => 0.5f,
            MaterialEnum.GemBloodstone => 0.3f,
            MaterialEnum.GemBlueDiamond => 1.2f,
            MaterialEnum.GemCanaryDiamond => 1.25f,
            MaterialEnum.GemDiamond => 1.1f,
            MaterialEnum.GemEmerald => 1f,
            MaterialEnum.GemFireAgate => 0.4f,
            MaterialEnum.GemFireOpal => 0.5f,
            MaterialEnum.GemFlourspar => 0.4f,
            MaterialEnum.GemGarnet => 0.3f,
            MaterialEnum.GemGreenstone => 0.1f,
            MaterialEnum.GemJacinth => 0.2f,
            MaterialEnum.GemKingsTear => 0.25f,
            MaterialEnum.GemMalachite => 0.2f,
            MaterialEnum.GemObsidian => 0.4f,
            MaterialEnum.GemPhenalope => 0.3f,
            MaterialEnum.GemRogueStone => 0.5f,
            MaterialEnum.GemRuby => 1f,
            MaterialEnum.GemSapphire => 1f,
            MaterialEnum.GemStarSapphire => 1.05f,
            MaterialEnum.GemTopaz => 0.6f,
            MaterialEnum.GemCrystalDeep => 0.4f,
            MaterialEnum.GemCrystalMundane => 0.1f,
            MaterialEnum.WoodPhandar => 0.2f,
            MaterialEnum.WoodShadowtop => 0.2f,
            MaterialEnum.WoodZhurkwood => 0.2f,
            MaterialEnum.Adamant => 0.4f,
            _ => 0f
        };
    }
}
