using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PlayerHouseService))]
public class PlayerHouseService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string HouseDoorTag = "db_house_door";

    private const string TargetAreaTagLocalString = "target_area_tag";

    private readonly IHouseRepository _houses;
    private readonly RuntimeCharacterService _characters;


    public PlayerHouseService(IHouseRepository houses, RuntimeCharacterService characters)
    {
        _houses = houses;
        _characters = characters;

        NwModule.Instance.OnModuleLoad += RegisterNewHouses;

        List<NwDoor> houseDoors = NwObject.FindObjectsWithTag<NwDoor>(HouseDoorTag).ToList();

        foreach (NwDoor door in houseDoors)
        {
            door.OnFailToOpen += HandlePlayerInteraction;
        }
    }

    private void RegisterNewHouses(ModuleEvents.OnModuleLoad obj)
    {
        List<NwArea> houses = NwModule.Instance.Areas.Where(a => a.LocalVariables.Any(lv => lv.Name == "is_house"))
            .ToList();

        foreach (NwArea house in houses)
        {
            LocalVariableInt isHouse = house.GetObjectVariable<LocalVariableInt>("is_house");

            if (isHouse.Value == 0) continue;
            House? savedHouse = _houses.GetHouseByTag(house.Tag);

            if (savedHouse is not null) continue;

            House newHouse = new()
            {
                Tag = house.Tag,
                Settlement = NWScript.GetLocalInt(house, "settlement"),
            };

            _houses.AddHouse(newHouse);
        }
    }

    private void HandlePlayerInteraction(DoorEvents.OnFailToOpen obj)
    {
        if (!obj.WhoFailed.IsPlayerControlled(out NwPlayer? player)) return;
        Log.Info($"{obj.WhoFailed.Name} is attempting to open door {obj.Door.Tag}");
        Guid key = _characters.GetPlayerKey(player);

        // Get the house area tag from the door's local variable
        string? targetAreaTag = obj.Door.GetObjectVariable<LocalVariableString>(TargetAreaTagLocalString).Value;

        if (string.IsNullOrEmpty(targetAreaTag))
        {
            Log.Warn($"Door {obj.Door.Tag} does not have a target_area_tag local variable");
            return;
        }

        // Get the house from the database
        House? house = _houses.GetHouseByTag(targetAreaTag);

        if (house is null)
        {
            Log.Warn($"House with tag {targetAreaTag} not found in database");
            return;
        }

        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.Tag == targetAreaTag);

        if (area is null)
        {
            Log.Error($"Area with tag {targetAreaTag} not found in module");
            return;
        }

        // Check if the house is unowned
        if (house.CharacterId == Guid.Empty)
        {
            // TODO: Implement purchase dialog/system
            player.FloatingTextString("This house is unowned. Purchase system coming soon!", false);
            return;
        }

        // Check if the player owns this house
        if (house.CharacterId == key)
        {
            obj.Door.Locked = false;
            obj.Door.Open();

            return;
        }

        // House belongs to someone else...Right now, we don't have the settlement system, so we just tell them it's not theirs.
        player.FloatingTextString("This house does not belong to you.", false);
    }
}
