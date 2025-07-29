using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(ItemCreator))]
public class ItemCreator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private Location? SystemSetupLocation { get; set; }

    public ItemCreator()
    {
        IEnumerable<NwWaypoint> nwWaypoints =
            NwObject.FindObjectsWithTag<NwWaypoint>("system_staging_spawn") as NwWaypoint[] ??
            NwObject.FindObjectsWithTag<NwWaypoint>("system_staging_spawn").ToArray();

        SystemSetupLocation = nwWaypoints.FirstOrDefault()?.Location;
    }

    public void GiveItemToPlayer(NwPlayer player, ItemType itemType, MaterialEnum material, QualityEnum quality,
        int amount)
    {
        if (SystemSetupLocation == null)
        {
            player.SendServerMessage(
                "Failed to spawn the item you were trying to get. Screenshot this and report as a bug",
                ColorConstants.Red);
            Log.Error("No system_staging_spawn found");
            return;
        }

        switch (itemType)
        {
            case ItemType.Ore:
                GiveOre(player, material, quality, amount);
                break;
            case ItemType.Armor:
                break;
            case ItemType.Weapon:
                break;
            case ItemType.Gem:
                break;
            case ItemType.Geode:
                break;
            case ItemType.Ingot:
                break;
            case ItemType.Log:
                break;
            case ItemType.Plank:
                break;
            case ItemType.FoodIngredient:
                break;
            case ItemType.Food:
                break;
            case ItemType.Drink:
                break;
            case ItemType.PotionIngredient:
                break;
            case ItemType.Potion:
                break;
            case ItemType.Grain:
                break;
            case ItemType.Flour:
                break;
            case ItemType.Scholastic:
                break;
            case ItemType.Pelt:
                break;
            case ItemType.Hide:
                break;
            case ItemType.Crafts:
                break;
            case ItemType.Stone:
                break;
            case ItemType.Unknown:
                break;
            case ItemType.Ammunition:
                break;
            default:
                Log.Error($"{itemType} is not supported yet.");
                break;
        }
    }

    private void GiveOre(NwPlayer player, MaterialEnum material, QualityEnum quality, int amount)
    {
        if (SystemSetupLocation == null)
        {
            return;
        }

        NwItem? ore = NwItem.Create("econ_ore", SystemSetupLocation, false, amount, $"ore_{material}".ToLower());

        if (ore == null) return;
        
        ore.Name = $"{material} Ore";
        // NWScript.CopyItemAndModify(ore, NWScript.)
    } 
}