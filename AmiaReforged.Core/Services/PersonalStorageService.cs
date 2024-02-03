using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(PersonalStorageService))]
public class PersonalStorageService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly DatabaseContextFactory _factory;
    private readonly CharacterService _characterService;
    private readonly NwTaskHelper _nwTaskHelper;

    public PersonalStorageService(DatabaseContextFactory factory, CharacterService characterService, NwTaskHelper nwTaskHelper)
    {
        _factory = factory;
        _characterService = characterService;
        _nwTaskHelper = nwTaskHelper;
        
    }

    private async Task HandleHomeStorage(NwItem? key, NwPlayer player, NwPlaceable chest)
    {
        if (key != null)
        {
            IEnumerable<StoredItem> dbItems = await GetStoredItems(player);

            foreach (StoredItem item in dbItems)
            {
                NwItem? parsed = Json.Parse(item.ItemJson).ToNwObject<NwItem>(chest.Location, chest);
                NWScript.SetLocalString(parsed, "db_guid", item.Id.ToString());
            }
        }
        else
        {
            NWScript.SendMessageToPC(player.LoginCreature, "You do not have a key to this storage.");
        }
        
        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private async void AddStoredItem(OnInventoryItemAdd obj)
    {
        if(NWScript.GetLocalInt(obj.AcquiredBy, "populatingChest") == 1) return;
        Log.Info("!! Storage on add handling !!");
        uint chestOwner = NWScript.GetLastUsedBy();
        NwCreature? chestOwnerCreature = chestOwner.ToNwObject<NwCreature>();
        NWScript.SendMessageToPC(chestOwnerCreature, "Adding item to storage.");
        NwItem? pcKey = chestOwnerCreature?.FindItemWithTag("ds_pckey");
        bool wasGold = ReturnGoldToUser(obj, chestOwner);
        if(wasGold) return;
        Guid characterId = Guid.Parse(pcKey.Name.Split("_")[1]);

        StoredItem newItem = new StoredItem();

        Json itemJson = NWScript.ObjectToJson(obj.Item);
        newItem.ItemJson = itemJson.Dump();
        newItem.Id = Guid.NewGuid();
        newItem.PlayerCharacterId = characterId;

        Log.Info($"ownername: {chestOwnerCreature?.Name}");

        bool success = true;
        
        AmiaDbContext amiaDbContext = _factory.CreateDbContext();
        try
        {
            await amiaDbContext.AddAsync(newItem);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            success = false;
            Log.Error($"Storage chest error: Could not add item. Exception: {e.Message} \n \n{e.InnerException}\n\n{e.StackTrace}");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        
        if (!success)
        {
            NWScript.SendMessageToPC(chestOwner, "There was an error storing your item.");
            NWScript.CopyItem(obj.Item, chestOwner, NWScript.TRUE);
            obj.Item.Destroy();
        }
        else
        {
            NWScript.SetLocalString(obj.Item, "db_guid", newItem.Id.ToString());
        }

    }

    private static bool ReturnGoldToUser(OnInventoryItemAdd obj, uint chestOwner)
    {
        if (obj.Item.ResRef != "nw_it_gold001") return false;

        int goldAmount = NWScript.GetItemStackSize(obj.Item);

        NWScript.GiveGoldToCreature(chestOwner, goldAmount);
        NWScript.SendMessageToPC(chestOwner, "You cannot store gold in your chest.");
        obj.Item.Destroy();
        return true;
    }

    private async void RemoveStoredItem(OnInventoryItemRemove obj)
    {
        if(NWScript.GetLocalInt(obj.RemovedFrom, "clearingChest") == 1) return;
        Log.Info("!! Storage on remove handling !!");
        
        string guid = NWScript.GetLocalString(obj.Item, "db_guid");
        if (guid is "") return;
        Guid itemId;

        try
        {
            itemId = Guid.Parse(guid);
        }
        catch
        {
            Log.Error("Storage chest error: Could not parse item GUID.");
            return;
        }

        AmiaDbContext amiaDbContext = _factory.CreateDbContext();
        StoredItem? s = await amiaDbContext.PlayerItems.FindAsync(itemId);
        if (s != null)
        {
            amiaDbContext.PlayerItems.Remove(s);
            await amiaDbContext.SaveChangesAsync();
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<IEnumerable<StoredItem>> GetStoredItems(NwPlayer player)
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        Log.Info($"Getting Stored Items. . .\n\tPlayer: {player.LoginCreature?.Name}\n\tPlayerKey: {pcKey?.Name}");
        player.SendServerMessage($"pcKey: {pcKey?.Name}");
        PlayerCharacter? character = await _characterService.GetCharacterFromPcKey(pcKey);
        await _nwTaskHelper.TrySwitchToMainThread();
        return character?.Items ?? new List<StoredItem>();
    }

    [ScriptHandler("storage_pc")]
    public void HandleOnClick(CallInfo info)
    {
        NwPlaceable? chest = info.ObjectSelf?.ObjectId.ToNwObject<NwPlaceable>();
        if (chest == null) return;

        chest.OnOpen += PopulateChest;
        chest.OnInventoryItemAdd += AddStoredItem;
        chest.OnInventoryItemRemove += RemoveStoredItem;
        chest.OnClose += close =>
        {
            DoesChestHaveOwnerId(close.Placeable, out bool hasOwnerId);
            string ownerId = NWScript.GetLocalString(close.Placeable.Area, "pc_home_id");
            NWScript.SetLocalInt(close.Placeable, "clearingChest", 1);
            close.Placeable.Locked = true;
            close.Placeable.LockKeyRequired = true;
            if(hasOwnerId) NWScript.SetLockKeyTag(close.Placeable, "");

            close.Placeable.Inventory.Items.ToList().ForEach(x => x.Destroy());

            if (!hasOwnerId)
            {
                NWScript.DelayCommand(6.0f, () => close.Placeable.Locked = false);
            }
            else
            {
                NWScript.DelayCommand(6.0f, () => NWScript.SetLockKeyTag(close.Placeable, ownerId));
            }
            NWScript.DelayCommand(6.0f, () => NWScript.SetLocalInt(close.Placeable, "clearingChest", 0));
        };

        //TODO: Remove when done debugging.
        Log.Info("!! Storage on clicked handling !!");
        NWScript.SetEventScript(chest, NWScript.EVENT_SCRIPT_PLACEABLE_ON_USED, "");
    }

    private async void PopulateChest(PlaceableEvents.OnOpen obj)
    { 
        if(obj.OpenedBy == null) return;
        
        NwPlaceable chest = obj.Placeable;
        NWScript.SetLocalInt(chest, "populatingChest", 1);
        
        NwPlayer? player = obj.OpenedBy.ControllingPlayer;
        NwItem? pcKey = player?.LoginCreature?.FindItemWithTag("ds_pckey");
        
        string ownerId = DoesChestHaveOwnerId(obj.Placeable, out bool hasOwnerId);

        Log.Info($"OnOpen info\n\townerId: {ownerId}\n\tplayerKey: {pcKey?.Name}\n\thasOwnerId: {hasOwnerId}");
        
        switch (hasOwnerId)
        {
            case true:
            {
                Log.Info("\tHas owner ID. Checking for key.");
                if (pcKey != null) NWScript.SetLocalString(chest, "chest_owner", pcKey.Name);
                NwItem? key = player?.LoginCreature?.FindItemWithTag(ownerId);
            
                await HandleHomeStorage(key, player!, chest);
                break;
            }
            case false when pcKey != null:
            {
                Log.Info("\tNo owner ID. Checking for key.");
                NWScript.SetLocalString(chest, "chest_owner", pcKey.Name);

                if (player != null)
                {
                    IEnumerable<StoredItem> dbItems = await GetStoredItems(player);
                    foreach (StoredItem item in dbItems)
                    {
                        Log.Info($"{item.Character.FirstName} has {item.Id} in their storage.");

                        NwObject? itemParsed = Json.Parse(item.ItemJson).ToNwObject<NwItem>(chest.Location, chest);
                        NWScript.SetLocalString(itemParsed, "db_guid", item.Id.ToString());
                    }
                }

                break;
            }
        }
        
        await _nwTaskHelper.TrySwitchToMainThread();
        
        NWScript.SetLocalInt(chest, "populatingChest", 0);
    }

    private static string DoesChestHaveOwnerId(NwPlaceable obj, out bool hasOwnerId)
    {
        string ownerId = NWScript.GetLocalString(obj.Area, "pc_home_id");
        hasOwnerId = ownerId != "";
        return ownerId;
    }
}