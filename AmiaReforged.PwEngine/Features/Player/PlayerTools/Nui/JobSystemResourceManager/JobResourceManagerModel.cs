using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.JobSystemResourceManager;

internal sealed class JobResourceManagerModel
{
    private readonly NwPlayer _player;
    private NwItem? _jobJournal;
    private int _lastOtherPlayerBoxIndex = -1; // Track last box index used for other player transfers

    private const string JobJournalTag = "js_jobjournal";
    private const string PrimaryJobVar = "primaryjob";
    private const string SecondaryJobVar = "secondaryjob";
    private const string MerchantJobName = "Merchant";
    private const string MiniatureStorageBoxTag = "js_merc_2_targ";
    private const string JobResourcePrefix = "js_";

    public JobResourceManagerModel(NwPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// Loads all resources from Merchant boxes, miniature storage boxes, and inventory
    /// </summary>
    public List<ResourceDataRecord> LoadAllResources()
    {
        List<ResourceDataRecord> resources = new();

        // Find job journal
        _jobJournal = FindJobJournal();

        if (_jobJournal != null)
        {
            // Load Merchant box resources if player has Merchant job
            if (IsMerchant())
            {
                resources.AddRange(LoadMerchantBoxResources());
            }
        }

        // Load miniature storage box resources
        resources.AddRange(LoadMiniatureBoxResources());

        // Load loose inventory resources
        resources.AddRange(LoadInventoryResources());

        return resources;
    }

    private NwItem? FindJobJournal()
    {
        if (_player.ControlledCreature == null) return null;

        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == JobJournalTag)
                return item;
        }
        return null;
    }

    private bool IsMerchant()
    {
        if (_jobJournal == null) return false;

        LocalVariableString primary = _jobJournal.GetObjectVariable<LocalVariableString>(PrimaryJobVar);
        LocalVariableString secondary = _jobJournal.GetObjectVariable<LocalVariableString>(SecondaryJobVar);

        return (primary.HasValue && primary.Value == MerchantJobName) ||
               (secondary.HasValue && secondary.Value == MerchantJobName);
    }

    private int GetMaxMerchantBoxes()
    {
        if (_jobJournal == null) return 0;

        LocalVariableString primary = _jobJournal.GetObjectVariable<LocalVariableString>(PrimaryJobVar);

        // Primary Merchants get 30 boxes, Secondary Merchants get 10 boxes
        if (primary.HasValue && primary.Value == MerchantJobName)
            return 30;

        LocalVariableString secondary = _jobJournal.GetObjectVariable<LocalVariableString>(SecondaryJobVar);
        if (secondary.HasValue && secondary.Value == MerchantJobName)
            return 10;

        return 0;
    }

    private List<ResourceDataRecord> LoadMerchantBoxResources()
    {
        List<ResourceDataRecord> resources = new();

        if (_jobJournal == null) return resources;

        for (int i = 1; i <= 30; i++)
        {
            string resrefVar = $"storagebox{i}";
            string amountVar = $"storagebox{i}amount";
            string nameVar = $"storagename{i}";

            LocalVariableString resref = _jobJournal.GetObjectVariable<LocalVariableString>(resrefVar);
            LocalVariableInt amount = _jobJournal.GetObjectVariable<LocalVariableInt>(amountVar);
            LocalVariableString name = _jobJournal.GetObjectVariable<LocalVariableString>(nameVar);

            if (resref.HasValue && amount.HasValue && amount.Value > 0)
            {
                string resourceName = name.HasValue ? name.Value ?? resref.Value ?? "" : resref.Value ?? "";
                resources.Add(new ResourceDataRecord(
                    resourceName,
                    amount.Value,
                    resref.Value ?? "",
                    ResourceSource.MerchantBox,
                    i
                ));
            }
        }

        return resources;
    }

    private List<ResourceDataRecord> LoadMiniatureBoxResources()
    {
        List<ResourceDataRecord> resources = new();

        if (_player.ControlledCreature == null) return resources;

        int index = 0;

        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == MiniatureStorageBoxTag)
            {
                LocalVariableString resref = item.GetObjectVariable<LocalVariableString>("storagebox");
                LocalVariableInt count = item.GetObjectVariable<LocalVariableInt>("storageboxcount");
                LocalVariableString name = item.GetObjectVariable<LocalVariableString>("storageboxname");

                if (resref.HasValue && count.HasValue && count.Value > 0)
                {
                    string resourceName = name.HasValue ? name.Value ?? resref.Value ?? "" : resref.Value ?? "";
                    resources.Add(new ResourceDataRecord(
                        resourceName,
                        count.Value,
                        resref.Value ?? "",
                        ResourceSource.MiniatureBox,
                        index
                    ));
                }
                index++;
            }
        }

        return resources;
    }

    private List<ResourceDataRecord> LoadInventoryResources()
    {
        List<ResourceDataRecord> resources = new();

        if (_player.ControlledCreature == null) return resources;

        Dictionary<string, (string name, int count)> groupedResources = new();

        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.ResRef.StartsWith(JobResourcePrefix) && item.Tag != MiniatureStorageBoxTag)
            {
                string resref = item.ResRef;
                if (groupedResources.ContainsKey(resref))
                {
                    var current = groupedResources[resref];
                    groupedResources[resref] = (current.name, current.count + item.StackSize);
                }
                else
                {
                    groupedResources[resref] = (item.Name, item.StackSize);
                }
            }
        }

        foreach (var kvp in groupedResources)
        {
            resources.Add(new ResourceDataRecord(
                kvp.Value.name,
                kvp.Value.count,
                kvp.Key,
                ResourceSource.Inventory,
                -1
            ));
        }

        return resources;
    }

    /// <summary>
    /// Transfers a resource from one location to another
    /// </summary>
    public bool TransferResource(ResourceDataRecord resource, int quantity,
        ResourceTransferDestination destination, NwPlayer? targetPlayer = null, int targetBoxIndex = -1)
    {
        if (quantity <= 0 || quantity > resource.Quantity)
            return false;

        // Remove from source
        if (!RemoveFromSource(resource, quantity))
            return false;

        // Add to destination
        if (!AddToDestination(resource.Resref, resource.Name, quantity, destination, targetPlayer, targetBoxIndex))
        {
            // Rollback: add back to source
            AddBackToSource(resource, quantity);
            return false;
        }

        return true;
    }

    private bool RemoveFromSource(ResourceDataRecord resource, int quantity)
    {
        switch (resource.Source)
        {
            case ResourceSource.MerchantBox:
                return RemoveFromMerchantBox(resource.SourceIndex, quantity);
            case ResourceSource.MiniatureBox:
                return RemoveFromMiniatureBox(resource.SourceIndex, quantity);
            case ResourceSource.Inventory:
                return RemoveFromInventory(resource.Resref, quantity);
            default:
                return false;
        }
    }

    private bool RemoveFromMerchantBox(int boxIndex, int quantity)
    {
        if (_jobJournal == null) return false;

        string amountVar = $"storagebox{boxIndex}amount";
        LocalVariableInt amount = _jobJournal.GetObjectVariable<LocalVariableInt>(amountVar);

        if (!amount.HasValue || amount.Value < quantity)
            return false;

        int newAmount = amount.Value - quantity;
        if (newAmount <= 0)
        {
            // Clear the storage box
            _jobJournal.GetObjectVariable<LocalVariableString>($"storagebox{boxIndex}").Delete();
            _jobJournal.GetObjectVariable<LocalVariableInt>(amountVar).Delete();
            _jobJournal.GetObjectVariable<LocalVariableString>($"storagename{boxIndex}").Delete();
        }
        else
        {
            amount.Value = newAmount;
        }

        return true;
    }

    private bool RemoveFromMiniatureBox(int boxIndex, int quantity)
    {
        if (_player.ControlledCreature == null) return false;

        int currentIndex = 0;
        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == MiniatureStorageBoxTag)
            {
                if (currentIndex == boxIndex)
                {
                    LocalVariableInt count = item.GetObjectVariable<LocalVariableInt>("storageboxcount");
                    if (!count.HasValue || count.Value < quantity)
                        return false;

                    int newCount = count.Value - quantity;
                    if (newCount <= 0)
                    {
                        // Clear the miniature box
                        item.GetObjectVariable<LocalVariableString>("storagebox").Delete();
                        item.GetObjectVariable<LocalVariableInt>("storageboxcount").Delete();
                        item.GetObjectVariable<LocalVariableString>("storageboxname").Delete();
                        item.Description = "An empty miniature storage box.";
                    }
                    else
                    {
                        count.Value = newCount;
                        item.Description = $"Item Count Stored: {newCount}";
                    }
                    return true;
                }
                currentIndex++;
            }
        }
        return false;
    }

    private bool RemoveFromInventory(string resref, int quantity)
    {
        if (_player.ControlledCreature == null) return false;

        int remaining = quantity;
        List<NwItem> toDestroy = new();

        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.ResRef == resref && remaining > 0)
            {
                if (item.StackSize <= remaining)
                {
                    remaining -= item.StackSize;
                    toDestroy.Add(item);
                }
                else
                {
                    item.StackSize -= remaining;
                    remaining = 0;
                    break;
                }
            }
        }

        foreach (NwItem item in toDestroy)
        {
            item.Destroy();
        }

        return remaining == 0;
    }

    private bool AddToDestination(string resref, string name, int quantity,
        ResourceTransferDestination destination, NwPlayer? targetPlayer, int targetBoxIndex)
    {
        switch (destination)
        {
            case ResourceTransferDestination.SelfMerchantBox:
                return AddToMerchantBox(resref, name, quantity);
            case ResourceTransferDestination.OtherPlayerMerchantBox:
                return AddToPlayerMerchantBox(targetPlayer, resref, name, quantity);
            case ResourceTransferDestination.MiniatureBox:
                return AddToMiniatureBox(targetBoxIndex, resref, name, quantity);
            case ResourceTransferDestination.Inventory:
                return AddToInventory(resref, name, quantity);
            default:
                return false;
        }
    }

    private bool AddToMerchantBox(string resref, string name, int quantity)
    {
        if (_jobJournal == null || !IsMerchant()) return false;

        int maxBoxes = GetMaxMerchantBoxes();

        // Find existing box with this resource
        for (int i = 1; i <= maxBoxes; i++)
        {
            string resrefVar = $"storagebox{i}";
            LocalVariableString boxResref = _jobJournal.GetObjectVariable<LocalVariableString>(resrefVar);

            if (boxResref.HasValue && boxResref.Value == resref)
            {
                // Add to existing box
                LocalVariableInt amount = _jobJournal.GetObjectVariable<LocalVariableInt>($"storagebox{i}amount");
                amount.Value = (amount.HasValue ? amount.Value : 0) + quantity;
                return true;
            }
        }

        // No existing box found - find first empty slot
        for (int i = 1; i <= maxBoxes; i++)
        {
            string resrefVar = $"storagebox{i}";
            LocalVariableString boxResref = _jobJournal.GetObjectVariable<LocalVariableString>(resrefVar);

            if (!boxResref.HasValue || boxResref.Value == "")
            {
                // Use this empty slot - initialize all three variables
                boxResref.Value = resref;
                _jobJournal.GetObjectVariable<LocalVariableInt>($"storagebox{i}amount").Value = quantity;
                _jobJournal.GetObjectVariable<LocalVariableString>($"storagename{i}").Value = name;
                return true;
            }
        }

        return false; // No available slots
    }

    private bool AddToPlayerMerchantBox(NwPlayer? targetPlayer, string resref, string name, int quantity)
    {
        _lastOtherPlayerBoxIndex = -1; // Reset before attempting

        if (targetPlayer == null || targetPlayer.ControlledCreature == null) return false;

        // Find target player's job journal
        NwItem? targetJournal = null;
        foreach (NwItem item in targetPlayer.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == JobJournalTag)
            {
                targetJournal = item;
                break;
            }
        }

        if (targetJournal == null) return false;

        // Check if target is a merchant
        LocalVariableString primary = targetJournal.GetObjectVariable<LocalVariableString>(PrimaryJobVar);
        LocalVariableString secondary = targetJournal.GetObjectVariable<LocalVariableString>(SecondaryJobVar);

        bool isMerchant = (primary.HasValue && primary.Value == MerchantJobName) ||
                         (secondary.HasValue && secondary.Value == MerchantJobName);

        if (!isMerchant) return false;

        // Determine max boxes for target player
        int maxBoxes = 0;
        if (primary.HasValue && primary.Value == MerchantJobName)
            maxBoxes = 30; // Primary merchant
        else if (secondary.HasValue && secondary.Value == MerchantJobName)
            maxBoxes = 10; // Secondary merchant

        // Find existing box with this resource
        for (int i = 1; i <= maxBoxes; i++)
        {
            string resrefVar = $"storagebox{i}";
            LocalVariableString boxResref = targetJournal.GetObjectVariable<LocalVariableString>(resrefVar);

            if (boxResref.HasValue && boxResref.Value == resref)
            {
                LocalVariableInt amount = targetJournal.GetObjectVariable<LocalVariableInt>($"storagebox{i}amount");
                amount.Value = (amount.HasValue ? amount.Value : 0) + quantity;
                _lastOtherPlayerBoxIndex = i; // Store the box index
                return true;
            }
        }

        // Find empty slot
        for (int i = 1; i <= maxBoxes; i++)
        {
            string resrefVar = $"storagebox{i}";
            LocalVariableString boxResref = targetJournal.GetObjectVariable<LocalVariableString>(resrefVar);

            if (!boxResref.HasValue || boxResref.Value == "")
            {
                boxResref.Value = resref;
                targetJournal.GetObjectVariable<LocalVariableInt>($"storagebox{i}amount").Value = quantity;
                targetJournal.GetObjectVariable<LocalVariableString>($"storagename{i}").Value = name;
                _lastOtherPlayerBoxIndex = i; // Store the box index
                return true;
            }
        }

        return false;
    }

    private bool AddToMiniatureBox(int boxIndex, string resref, string name, int quantity)
    {
        if (_player.ControlledCreature == null) return false;

        int currentIndex = 0;
        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == MiniatureStorageBoxTag)
            {
                if (currentIndex == boxIndex)
                {
                    LocalVariableString boxResref = item.GetObjectVariable<LocalVariableString>("storagebox");
                    LocalVariableInt count = item.GetObjectVariable<LocalVariableInt>("storageboxcount");

                    if (!boxResref.HasValue || boxResref.Value == resref || boxResref.Value == "")
                    {
                        boxResref.Value = resref;
                        count.Value = (count.HasValue ? count.Value : 0) + quantity;
                        item.GetObjectVariable<LocalVariableString>("storageboxname").Value = name;
                        item.Description = $"Item Count Stored: {count.Value}";
                        item.Name = $"<c~Îë>Storage Chest: {name}</c>";
                        return true;
                    }
                    return false; // Box already has different resource
                }
                currentIndex++;
            }
        }
        return false;
    }

    private bool AddToInventory(string resref, string name, int quantity)
    {
        if (_player.ControlledCreature?.Location == null) return false;

        // Create items one at a time and acquire them
        // This handles both stackable and non-stackable items correctly
        for (int i = 0; i < quantity; i++)
        {
            NwItem? created = NwItem.Create(resref, _player.ControlledCreature.Location);
            if (created == null) return false;

            // Acquire the item into the player's inventory
            _player.ControlledCreature.AcquireItem(created);
        }

        return true;
    }

    private void AddBackToSource(ResourceDataRecord resource, int quantity)
    {
        // Rollback operation - add back to original source
        switch (resource.Source)
        {
            case ResourceSource.MerchantBox:
                AddToMerchantBoxAtIndex(resource.SourceIndex, resource.Resref, resource.Name, quantity);
                break;
            case ResourceSource.MiniatureBox:
                AddToMiniatureBox(resource.SourceIndex, resource.Resref, resource.Name, quantity);
                break;
            case ResourceSource.Inventory:
                AddToInventory(resource.Resref, resource.Name, quantity);
                break;
        }
    }

    private void AddToMerchantBoxAtIndex(int index, string resref, string name, int quantity)
    {
        if (_jobJournal == null) return;

        LocalVariableInt amount = _jobJournal.GetObjectVariable<LocalVariableInt>($"storagebox{index}amount");
        amount.Value = (amount.HasValue ? amount.Value : 0) + quantity;
        _jobJournal.GetObjectVariable<LocalVariableString>($"storagebox{index}").Value = resref;
        _jobJournal.GetObjectVariable<LocalVariableString>($"storagename{index}").Value = name;
    }

    public int GetAvailableMerchantBoxSlots()
    {
        if (_jobJournal == null || !IsMerchant()) return 0;

        int maxBoxes = GetMaxMerchantBoxes();
        int usedSlots = 0;

        for (int i = 1; i <= maxBoxes; i++)
        {
            LocalVariableString resref = _jobJournal.GetObjectVariable<LocalVariableString>($"storagebox{i}");
            if (resref.HasValue && resref.Value != "")
                usedSlots++;
        }

        return maxBoxes - usedSlots;
    }

    public List<NwItem> GetMiniatureStorageBoxes()
    {
        List<NwItem> boxes = new();

        if (_player.ControlledCreature == null) return boxes;

        foreach (NwItem item in _player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == MiniatureStorageBoxTag)
                boxes.Add(item);
        }
        return boxes;
    }

    public bool IsMerchantJob() => IsMerchant();

    public bool HasMerchantBoxWithResource(string resref)
    {
        if (_jobJournal == null) return false;

        for (int i = 1; i <= 30; i++)
        {
            LocalVariableString boxResref = _jobJournal.GetObjectVariable<LocalVariableString>($"storagebox{i}");
            if (boxResref.HasValue && boxResref.Value == resref)
            {
                return true;
            }
        }
        return false;
    }

    public int GetLastOtherPlayerBoxIndex()
    {
        return _lastOtherPlayerBoxIndex;
    }
}

