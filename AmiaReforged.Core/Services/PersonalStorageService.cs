using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Newtonsoft.Json.Linq;
using NLog;
using NWN.Core;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(PersonalStorageService))]
public class PersonalStorageService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly AmiaDbContext _ctx;
    private readonly CharacterService _characterService;
    private readonly NwTaskHelper _nwTaskHelper;

    public PersonalStorageService(AmiaDbContext ctx, CharacterService characterService, NwTaskHelper nwTaskHelper)
    {
        _ctx = ctx;
        _characterService = characterService;
        _nwTaskHelper = nwTaskHelper;
    }
    
    private async void PopulateChest(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;
        if (obj.UsedBy != player.LoginCreature) return;
        //Check if it has an owner variable. If UsedBy does not equal owner, return.
        NwPlaceable chest = obj.Placeable;
        NwCreature chestOwner = obj.UsedBy;
        string ownerPCKey = NWScript.GetName(NWScript.GetItemPossessedBy(chestOwner, "ds_pckey"));
        string ownerID = NWScript.GetLocalString(obj.Placeable.Area, "pc_home_id");
        if (ownerID != "")
        {
            NWScript.SetLocalString(chest, "chest_owner", ownerPCKey);
            if (player.LoginCreature.Inventory.Items.Where(x => x.Tag == ownerID).ToList().Count == 1)
            {
                IEnumerable<StoredItem> dbItems = await GetStoredItems(player);
                
                foreach (var item in dbItems)
                {
                    NwItem? parsed = Json.Parse(item.ItemJson).ToNwObject<NwItem>(chest.Location, chest);
                    NWScript.SetLocalString(parsed, "db_guid", item.Id.ToString());
                }
            }
            NWScript.SendMessageToPC(player.LoginCreature, "You do not have a key to this storage.");
        }

        if (ownerID == "")
        {
            NWScript.SetLocalString(chest, "chest_owner", ownerPCKey);
            IEnumerable<StoredItem> dbItems = await GetStoredItems(player);
                
            foreach (var item in dbItems)
            {
                Json.Parse(item.ItemJson).ToNwObject<NwItem>(chest.Location, chest);
            }
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private async void AddStoredItem(OnInventoryItemAdd obj)
    {
        string ownerCheck = NWScript.GetLocalString(obj.AcquiredBy, "chest_owner");
        uint chestOwner = NWScript.GetLastUsedBy();
        NwItem? pcKey = NWScript.GetItemPossessedBy(chestOwner, "ds_pckey").ToNwObject<NwItem>();
        ReturnGoldToUser(obj, chestOwner);

        StoredItem newItem = new StoredItem();

        Json itemJson = NWScript.ObjectToJson(obj.Item);
        newItem.ItemJson = itemJson.Dump();
        newItem.Id = Guid.NewGuid();
        newItem.Character = (await _characterService.GetCharacterFromPcKey(pcKey!))!;
        newItem.PlayerCharacterId = Guid.Parse(pcKey.Name.Split("_")[1]);

        try
        {
            await _ctx.AddAsync(newItem);
        }
        catch
        {
            Log.Error("Storage chest error: Could not add item.");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private static void ReturnGoldToUser(OnInventoryItemAdd obj, uint chestOwner)
    {
        if (obj.Item.ResRef != "nw_it_gold001") return;
        
        int GoldAmount = NWScript.GetItemStackSize(obj.Item);
        
        NWScript.GiveGoldToCreature(chestOwner, GoldAmount);
        NWScript.SendMessageToPC(chestOwner, "You cannot store gold in your chest.");
        NWScript.DestroyObject(obj.Item);
    }

    private async void RemoveStoredItem(OnInventoryItemRemove obj)
    {
        var guid = NWScript.GetLocalString(obj.RemovedFrom, "db_guid");
        if(guid is "") return;
        Guid itemID = Guid.NewGuid();
        
        try
        {
            itemID = Guid.Parse(guid);
        }
        catch
        {
            Log.Error("Storage chest error: Could not parse item GUID.");
            return;
        }
        
        StoredItem? s = await _ctx.PlayerItems.FindAsync(itemID);
        if (s != null) _ctx.PlayerItems.Remove(s);

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<IEnumerable<StoredItem>> GetStoredItems(NwPlayer player)
    {
        NwItem pcKey = player.LoginCreature!.Inventory.Items.First(x => x.Tag == "ds_pckey");
        PlayerCharacter? character = await _characterService.GetCharacterFromPcKey(pcKey);
        await _nwTaskHelper.TrySwitchToMainThread();
        return character?.Items ?? new List<StoredItem>();
    }
    [ScriptHandler("storage_pc")]
    public void HandleHeartbeat(CallInfo info)
    {
        NwPlaceable chest = info.ObjectSelf.ObjectId.ToNwObject<NwPlaceable>();
        chest.OnUsed += PopulateChest;
        chest.OnInventoryItemAdd += AddStoredItem;
        chest.OnInventoryItemRemove += RemoveStoredItem;
        
        //TODO: Remove when done debugging.
        Log.Info("!! Storage chest Heartbeat. !!");
        NWScript.SetEventScript(chest, 9004, "");
    }
}