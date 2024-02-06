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
    private const string StorageCap = "storage_cap";
    private const string DsPckey = "ds_pckey";
    private const int InitialBankCapacity = 20;
    private const string ChestInUse = "in_use";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly DatabaseContextFactory _factory;
    private readonly CharacterService _characterService;
    private readonly NwTaskHelper _nwTaskHelper;

    public PersonalStorageService(DatabaseContextFactory factory, CharacterService characterService,
        NwTaskHelper nwTaskHelper)
    {
        _factory = factory;
        _characterService = characterService;
        _nwTaskHelper = nwTaskHelper;
        List<NwPlaceable> chests = NwObject.FindObjectsWithTag<NwPlaceable>("db_pcstorage").ToList();
        foreach (NwPlaceable chest in chests)
        {
            chest.OnOpen += PopulateChest;
            chest.OnInventoryItemAdd += AddStoredItem;
            chest.OnInventoryItemRemove += RemoveStoredItem;
            chest.OnClose += close =>
            {
                DoesChestHaveOwnerId(close.Placeable, out bool hasOwnerId);
                string ownerId = NWScript.GetLocalString(close.Placeable, "pc_home_id");
                NWScript.SetLocalInt(close.Placeable, "clearingChest", 1);
                close.Placeable.Locked = true;
                close.Placeable.LockKeyRequired = true;
                if (hasOwnerId) NWScript.SetLockKeyTag(close.Placeable, "nostoragekey");

                close.Placeable.Inventory.Items.ToList().ForEach(x => x.Destroy());

                if (!hasOwnerId)
                {
                    NWScript.DelayCommand(6.0f, () => close.Placeable.Locked = false);
                }
                else
                {
                    NWScript.DelayCommand(6.0f, () => NWScript.SetLockKeyTag(close.Placeable, ownerId));
                }

                NWScript.DelayCommand(6.0f, () => NWScript.SetLocalInt(close.Placeable, ChestInUse, 0));
                NWScript.DelayCommand(6.0f, () => NWScript.SetLocalInt(close.Placeable, "clearingChest", 0));
            };
        }
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
        if (NWScript.GetLocalInt(obj.AcquiredBy, "populatingChest") == 1) return;
        uint chestOwner = NWScript.GetLastUsedBy();

        NwCreature? chestOwnerCreature = chestOwner.ToNwObject<NwCreature>();
        NwPlaceable chest = (NwPlaceable)obj.AcquiredBy;
        NwItem? pcKey = chestOwnerCreature?.FindItemWithTag(DsPckey);

        int storageCap = NWScript.GetLocalInt(pcKey, StorageCap);

        bool wasGold = ReturnGoldToUser(obj, chestOwner);
        if (wasGold) return;

        if (chest.Inventory.Items.Count() + 1 > storageCap)
        {
            NWScript.CopyItem(obj.Item, chestOwner, NWScript.TRUE);
            obj.Item.Destroy();
            chestOwnerCreature?.ControllingPlayer?.SendServerMessage(
                $"You have have reached the maximum amount of items you can store. ({storageCap})");

            return;
        }

        Guid characterId = Guid.Parse(pcKey!.Name.Split("_")[1]);

        StoredItem newItem = new StoredItem();

        Json itemJson = NWScript.ObjectToJson(obj.Item);
        newItem.ItemJson = itemJson.Dump();
        newItem.Id = Guid.NewGuid();
        newItem.PlayerCharacterId = characterId;


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
            Log.Error(
                $"Storage chest error: Could not add item. Exception: {e.Message} \n \n{e.InnerException}\n\n{e.StackTrace}");
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
        if (NWScript.GetLocalInt(obj.RemovedFrom, "clearingChest") == 1) return;

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

    private async Task<IEnumerable<StoredItem>> GetStoredItems(NwPlayer player)
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag(DsPckey);
        player.SendServerMessage($"pcKey: {pcKey?.Name}");
        PlayerCharacter? character = await _characterService.GetCharacterFromPcKey(pcKey);
        await _nwTaskHelper.TrySwitchToMainThread();
        return character?.Items ?? new List<StoredItem>();
    }

    private async void PopulateChest(PlaceableEvents.OnOpen obj)
    {
        if (obj.OpenedBy == null) return;
        NwPlaceable chest = obj.Placeable;
        
        
        if (NWScript.GetLocalInt(chest, ChestInUse) == NWScript.TRUE)
        {
            NWScript.SendMessageToPC(obj.OpenedBy, "This chest is already in use.");
            NWScript.AssignCommand(obj.OpenedBy, () => NWScript.ActionMoveAwayFromObject(obj.OpenedBy, NWScript.TRUE));
            return;
        }
        NWScript.SetLocalInt(chest, "populatingChest", NWScript.TRUE);
        NWScript.SetLocalInt(chest, ChestInUse, NWScript.TRUE);

        NwPlayer? player = obj.OpenedBy.ControllingPlayer;
        NwItem? pcKey = player?.LoginCreature?.FindItemWithTag(DsPckey);

        InitIfNewBankMember(pcKey);

        string ownerId = DoesChestHaveOwnerId(obj.Placeable, out bool hasOwnerId);

        switch (hasOwnerId)
        {
            case true:
            {
                if (pcKey != null) NWScript.SetLocalString(chest, "chest_owner", pcKey.Name);
                NwItem? key = player?.LoginCreature?.FindItemWithTag(ownerId);

                await HandleHomeStorage(key, player!, chest);
                break;
            }
            case false when pcKey != null:
            {
                NWScript.SetLocalString(chest, "chest_owner", pcKey.Name);

                if (player != null)
                {
                    IEnumerable<StoredItem> dbItems = await GetStoredItems(player);
                    foreach (StoredItem item in dbItems)
                    {
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

    private static void InitIfNewBankMember(NwItem? pcKey)
    {
        int storageCapacity = NWScript.GetLocalInt(pcKey, StorageCap);

        if (storageCapacity == 0)
        {
            // Initialize new bank members with an initial capacity.
            NWScript.SetLocalInt(pcKey, StorageCap, InitialBankCapacity);
        }
    }

    private static string DoesChestHaveOwnerId(NwPlaceable obj, out bool hasOwnerId)
    {
        string ownerId = NWScript.GetLocalString(obj, "pc_home_id");
        hasOwnerId = ownerId != "";
        return ownerId;
    }
}