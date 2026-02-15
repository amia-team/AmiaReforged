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

    public PersonalStorageService(DatabaseContextFactory factory, CharacterService characterService)
    {
        _factory = factory;
        _characterService = characterService;
        List<NwPlaceable> chests = NwObject.FindObjectsWithTag<NwPlaceable>("db_pcstorage").ToList();
        foreach (NwPlaceable chest in chests)
        {
            chest.OnOpen += PopulateChest;
            chest.OnLeftClick += HandleChestUse;
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

    private void HandleChestUse(PlaceableEvents.OnLeftClick obj)
    {
        NwPlaceable chest = obj.Placeable;

        if (NWScript.GetLocalInt(chest, ChestInUse) == NWScript.TRUE)
        {
            NWScript.SendMessageToPC(obj.ClickedBy.ControlledCreature, "This chest is already in use.");
            NWScript.AssignCommand(obj.ClickedBy.ControlledCreature,
                () => NWScript.ActionMoveAwayFromObject(obj.ClickedBy.ControlledCreature, NWScript.TRUE));
        }
    }

    private async Task HandleHomeStorage(NwItem? key, NwPlayer player, NwPlaceable chest)
    {
        if (key != null)
        {
            IEnumerable<StoredItem> dbItems = await GetStoredItems(player);
            await NwTask.SwitchToMainThread();

            foreach (StoredItem item in dbItems)
            {
                NwItem? parsed = Json.Parse(item.ItemJson).ToNwObject<NwItem>(chest.Location, chest);
                NWScript.SetLocalString(parsed, "db_guid", item.ItemId.ToString());
            }
        }
        else
        {
            NWScript.SendMessageToPC(player.LoginCreature, "You do not have a key to this storage.");
        }
    }

    private async void AddStoredItem(OnInventoryItemAdd obj)
    {
        try
        {
            if (NWScript.GetLocalInt(obj.AcquiredBy, "populatingChest") == 1) return;
            uint chestOwner = NWScript.GetLastUsedBy();

            NwCreature? chestOwnerCreature = chestOwner.ToNwObject<NwCreature>();
            NwPlaceable chest = (NwPlaceable)obj.AcquiredBy;
            NwItem? pcKey = chestOwnerCreature?.FindItemWithTag(DsPckey);

            int storageCap = NWScript.GetLocalInt(pcKey, StorageCap);

            bool wasGold = ReturnGoldToUser(obj, chestOwner);
            if (wasGold) return;


            Json backup = obj.Item.SerializeToJson(true);
            if (chest.Inventory.Items.Count() + 1 > storageCap)
            {
                backup.ToNwObject<NwItem>(chestOwnerCreature.Location, chestOwnerCreature);
                obj.Item.Destroy();

                chestOwnerCreature?.ControllingPlayer?.SendServerMessage(
                    $"You have have reached the maximum amount of items you can store. ({storageCap})");


                return;
            }

            Guid characterId = Guid.Parse(pcKey!.Name.Split("_")[1]);


            Json itemJson = NWScript.ObjectToJson(obj.Item);
            StoredItem newItem = new()
            {
                ItemJson = itemJson.Dump(),
                PlayerCharacterId = characterId
            };


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

            await NwTask.SwitchToMainThread();

            if (!success)
            {
                NWScript.SendMessageToPC(chestOwner, "There was an error storing your item.");
                NWScript.CopyItem(obj.Item, chestOwner, NWScript.TRUE);
                obj.Item.Destroy();
            }
            else
            {
                NWScript.SetLocalString(obj.Item, "db_guid", newItem.ItemId.ToString());
            }

            NWScript.ExportSingleCharacter(chestOwner);
            NWScript.SendMessageToPC(chestOwner, "Your character has been saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AddStoredItem");
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
        try
        {
            if (NWScript.GetLocalInt(obj.RemovedFrom, "clearingChest") == 1) return;

            string storedId = NWScript.GetLocalString(obj.Item, "db_guid");
            if (storedId is "") return;
            long itemId;

            try
            {
                itemId = long.Parse(storedId);
            }
            catch
            {
                Log.Error("Storage chest error: Could not parse item ID.");
                return;
            }

            AmiaDbContext amiaDbContext = _factory.CreateDbContext();
            StoredItem? s = await amiaDbContext.PlayerItems.FindAsync(itemId);
            if (s != null)
            {
                amiaDbContext.PlayerItems.Remove(s);
                await amiaDbContext.SaveChangesAsync();
            }

            await NwTask.SwitchToMainThread();
            uint chestOwner = NWScript.GetLastUsedBy();
            NWScript.ExportSingleCharacter(chestOwner);
            NWScript.SendMessageToPC(chestOwner, "Your character has been saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in RemoveStoredItem");
        }
    }

    private async Task<IEnumerable<StoredItem>> GetStoredItems(NwPlayer player)
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag(DsPckey);
        player.SendServerMessage($"pcKey: {pcKey?.Name}");
        PlayerCharacter? character = await _characterService.GetCharacterFromPcKey(pcKey);
        await NwTask.SwitchToMainThread();
        return character?.Items ?? new List<StoredItem>();
    }

    private async void PopulateChest(PlaceableEvents.OnOpen obj)
    {
        try
        {
            if (obj.OpenedBy == null) return;
            NwPlaceable chest = obj.Placeable;

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
                    await NwTask.SwitchToMainThread();
                    break;
                }
                case false when pcKey != null:
                {
                    NWScript.SetLocalString(chest, "chest_owner", pcKey.Name);

                    if (player != null)
                    {
                        IEnumerable<StoredItem> dbItems = await GetStoredItems(player);
                        await NwTask.SwitchToMainThread();

                        foreach (StoredItem item in dbItems)
                        {
                            NwObject? itemParsed = Json.Parse(item.ItemJson).ToNwObject<NwItem>(chest.Location, chest);
                            NWScript.SetLocalString(itemParsed, "db_guid", item.ItemId.ToString());
                        }
                    }

                    break;
                }
            }

            NWScript.SetLocalInt(chest, "populatingChest", 0);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
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
