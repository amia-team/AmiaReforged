using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.JobSystemResourceManager;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.JobSystemResourceModifier;

public sealed class JobSystemResourceModifierPresenter : ScryPresenter<JobSystemResourceModifierView>
{
    public override JobSystemResourceModifierView View { get; }

    private readonly NwPlayer _dm;
    private NwPlayer? _selectedPlayer;
    private JobResourceManagerModel? _model;
    private List<ResourceDataRecord> _currentResources = new();
    private bool _processingEvent;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    public JobSystemResourceModifierPresenter(JobSystemResourceModifierView view, NwPlayer dm)
    {
        View = view;
        _dm = dm;
    }

    public override void InitBefore()
    {
        // No initialization needed before window creation
    }

    public override void Create()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(300f, 100f, 645f, 675f),
            Resizable = false,
        };

        if (!_dm.TryCreateNuiWindow(_window, out _token))
            return;

        // Initialize bindings
        Token().SetBindValue(View.SelectedPlayerName, "No player selected");
        Token().SetBindValue(View.PlayerSelected, false);
        Token().SetBindValue(View.ResourceCount, 0);
    }

    public override void Close()
    {
        Token().Close();
    }

    private void LoadResources()
    {
        if (_model == null || _selectedPlayer == null)
        {
            _currentResources = new List<ResourceDataRecord>();
            UpdateResourceListDisplay();
            return;
        }

        _currentResources = _model.LoadAllResources();
        UpdateResourceListDisplay();
    }

    private void UpdateResourceListDisplay()
    {
        List<string> names = new();
        List<string> quantities = new();
        List<string> sources = new();
        List<string> modifyQuantities = new();

        foreach (ResourceDataRecord resource in _currentResources)
        {
            names.Add(resource.Name);
            quantities.Add(resource.Quantity.ToString());
            sources.Add(GetSourceDisplayName(resource.Source));
            modifyQuantities.Add("1"); // Default modify quantity
        }

        Token().SetBindValue(View.ResourceCount, _currentResources.Count);
        Token().SetBindValues(View.ResourceNames, names);
        Token().SetBindValues(View.ResourceQuantities, quantities);
        Token().SetBindValues(View.ResourceSources, sources);
        Token().SetBindValues(View.ModifyQuantities, modifyQuantities);
    }

    private string GetSourceDisplayName(ResourceSource source)
    {
        return source switch
        {
            ResourceSource.MerchantBox => "Merchant Box",
            ResourceSource.MiniatureBox => "Miniature Box",
            ResourceSource.Inventory => "Inventory",
            _ => "Unknown"
        };
    }

    private void DelayedRefresh()
    {
        // Use NWScript.DelayCommand to schedule refresh on main game thread after 2 seconds
        NWScript.DelayCommand(2.0f, () =>
        {
            // Check if DM and selected player are still valid before refreshing
            if (_dm?.IsValid == true && _selectedPlayer?.IsValid == true)
            {
                try
                {
                    LoadResources();
                }
                catch (System.InvalidOperationException)
                {
                    // Window was closed, ignore the error
                }
            }
        });
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (_processingEvent)
            return;
        _processingEvent = true;

        try
        {
            // Filter out non-click events (like scroll) to prevent accidental button triggers
            if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.Open)
            {
                _processingEvent = false;
                return;
            }

            // Select Player button
            if (ev.ElementId == "btn_select_player")
            {
                _dm.SendServerMessage("Please select a player to view their resources.", new Color(0, 255, 255));
                _dm.EnterTargetMode(OnPlayerSelected, new()
                {
                    ValidTargets = ObjectTypes.Creature
                });
                ResetProcessingFlag();
                return;
            }

            // Refresh button
            if (ev.ElementId == "btn_refresh")
            {
                LoadResources();
                _dm.SendServerMessage("Resources refreshed.", new Color(0, 255, 0));
                ResetProcessingFlag();
                return;
            }

            // Close button
            if (ev.ElementId == "btn_close")
            {
                Close();
                return;
            }

            // Remove quantity button
            if (ev.ElementId == "btn_remove")
            {
                HandleRemoveQuantity(ev.ArrayIndex);
                ResetProcessingFlag();
                return;
            }

            // Add quantity button
            if (ev.ElementId == "btn_add")
            {
                HandleAddQuantity(ev.ArrayIndex);
                ResetProcessingFlag();
                return;
            }
        }
        finally
        {
            // Only reset if not already scheduled
            if (_processingEvent)
            {
                ResetProcessingFlag();
            }
        }
    }

    private void ResetProcessingFlag()
    {
        // Delay flag reset to prevent multiple rapid-fire events
        NWScript.DelayCommand(0.1f, () =>
        {
            _processingEvent = false;
        });
    }

    private void OnPlayerSelected(ModuleEvents.OnPlayerTarget targetEvent)
    {
        if (targetEvent.TargetObject is not NwCreature targetCreature)
        {
            _dm.SendServerMessage("Invalid target. Please select a player character.", new Color(255, 0, 0));
            return;
        }

        if (targetCreature.ControllingPlayer == null)
        {
            _dm.SendServerMessage("Target must be a player character.", new Color(255, 0, 0));
            return;
        }

        _selectedPlayer = targetCreature.ControllingPlayer;
        _model = new JobResourceManagerModel(_selectedPlayer);

        string characterName = targetCreature.Name;
        Token().SetBindValue(View.SelectedPlayerName, $"Selected Character: {characterName}");
        Token().SetBindValue(View.PlayerSelected, true);

        LoadResources();
        _dm.SendServerMessage($"Now viewing resources for {characterName}.", new Color(0, 255, 0));
    }

    private void HandleRemoveQuantity(int arrayIndex)
    {
        if (_selectedPlayer == null || _model == null)
        {
            _dm.SendServerMessage("No player selected.", new Color(255, 0, 0));
            return;
        }

        if (arrayIndex < 0 || arrayIndex >= _currentResources.Count)
            return;

        ResourceDataRecord resource = _currentResources[arrayIndex];

        // Get the modify quantity
        List<string> quantities = Token().GetBindValues(View.ModifyQuantities);
        if (arrayIndex >= quantities.Count)
            return;

        if (!int.TryParse(quantities[arrayIndex], out int quantity) || quantity <= 0)
        {
            _dm.SendServerMessage("Invalid quantity.", new Color(255, 0, 0));
            return;
        }

        if (quantity > resource.Quantity)
        {
            _dm.SendServerMessage($"Cannot remove more than available ({resource.Quantity}).", new Color(255, 0, 0));
            return;
        }

        // Remove from source
        bool success = resource.Source switch
        {
            ResourceSource.MerchantBox => RemoveFromMerchantBox(resource, quantity),
            ResourceSource.MiniatureBox => RemoveFromMiniatureBox(resource, quantity),
            ResourceSource.Inventory => RemoveFromInventory(resource, quantity),
            _ => false
        };

        if (success)
        {
            string characterName = _selectedPlayer.ControlledCreature?.Name ?? "Unknown";
            _dm.SendServerMessage($"Removed {quantity} {resource.Name} from {characterName}'s {GetSourceDisplayName(resource.Source)}.", new Color(0, 255, 0));
            DelayedRefresh();
        }
        else
        {
            _dm.SendServerMessage("Failed to remove resources.", new Color(255, 0, 0));
        }
    }

    private void HandleAddQuantity(int arrayIndex)
    {
        if (_selectedPlayer == null || _model == null)
        {
            _dm.SendServerMessage("No player selected.", new Color(255, 0, 0));
            return;
        }

        if (arrayIndex < 0 || arrayIndex >= _currentResources.Count)
            return;

        ResourceDataRecord resource = _currentResources[arrayIndex];

        // Get the modify quantity
        List<string> quantities = Token().GetBindValues(View.ModifyQuantities);
        if (arrayIndex >= quantities.Count)
            return;

        if (!int.TryParse(quantities[arrayIndex], out int quantity) || quantity <= 0)
        {
            _dm.SendServerMessage("Invalid quantity.", new Color(255, 0, 0));
            return;
        }

        // Add to source
        bool success = resource.Source switch
        {
            ResourceSource.MerchantBox => AddToMerchantBox(resource, quantity),
            ResourceSource.MiniatureBox => AddToMiniatureBox(resource, quantity),
            ResourceSource.Inventory => AddToInventory(resource, quantity),
            _ => false
        };

        if (success)
        {
            string characterName = _selectedPlayer.ControlledCreature?.Name ?? "Unknown";
            _dm.SendServerMessage($"Added {quantity} {resource.Name} to {characterName}'s {GetSourceDisplayName(resource.Source)}.", new Color(0, 255, 0));
            DelayedRefresh();
        }
        else
        {
            _dm.SendServerMessage("Failed to add resources.", new Color(255, 0, 0));
        }
    }

    private bool RemoveFromMerchantBox(ResourceDataRecord resource, int quantity)
    {
        if (_selectedPlayer?.ControlledCreature == null) return false;

        NwItem? jobJournal = FindJobJournal(_selectedPlayer);
        if (jobJournal == null) return false;

        int boxIndex = resource.SourceIndex;
        string amountVar = $"storagebox{boxIndex}amount";
        LocalVariableInt amount = jobJournal.GetObjectVariable<LocalVariableInt>(amountVar);

        if (!amount.HasValue || amount.Value < quantity)
            return false;

        int newAmount = amount.Value - quantity;
        if (newAmount <= 0)
        {
            // Clear the storage box completely
            jobJournal.GetObjectVariable<LocalVariableString>($"storagebox{boxIndex}").Delete();
            jobJournal.GetObjectVariable<LocalVariableInt>(amountVar).Delete();
            jobJournal.GetObjectVariable<LocalVariableString>($"storagename{boxIndex}").Delete();
        }
        else
        {
            amount.Value = newAmount;
        }

        return true;
    }

    private bool RemoveFromMiniatureBox(ResourceDataRecord resource, int quantity)
    {
        if (_selectedPlayer?.ControlledCreature == null) return false;

        List<NwItem> boxes = GetMiniatureStorageBoxes(_selectedPlayer);
        int boxIndex = resource.SourceIndex;

        if (boxIndex < 0 || boxIndex >= boxes.Count) return false;

        NwItem box = boxes[boxIndex];
        LocalVariableInt count = box.GetObjectVariable<LocalVariableInt>("storageboxcount");

        if (!count.HasValue || count.Value < quantity)
            return false;

        int newCount = count.Value - quantity;
        if (newCount <= 0)
        {
            // Clear the miniature box completely
            box.GetObjectVariable<LocalVariableString>("storagebox").Delete();
            box.GetObjectVariable<LocalVariableInt>("storageboxcount").Delete();
            box.GetObjectVariable<LocalVariableString>("storageboxname").Delete();
            box.Description = "An empty miniature storage box.";
            box.Name = "<c~Îë>Empty Miniature Storage Box</c>";
        }
        else
        {
            count.Value = newCount;
            box.Description = $"Item Count Stored: {newCount}";
        }

        return true;
    }

    private bool RemoveFromInventory(ResourceDataRecord resource, int quantity)
    {
        if (_selectedPlayer?.ControlledCreature == null) return false;

        int remaining = quantity;
        List<NwItem> toDestroy = new();

        foreach (NwItem item in _selectedPlayer.ControlledCreature.Inventory.Items)
        {
            if (item.ResRef == resource.Resref && remaining > 0)
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

    private bool AddToMerchantBox(ResourceDataRecord resource, int quantity)
    {
        if (_selectedPlayer?.ControlledCreature == null) return false;

        NwItem? jobJournal = FindJobJournal(_selectedPlayer);
        if (jobJournal == null) return false;

        int boxIndex = resource.SourceIndex;
        LocalVariableInt amount = jobJournal.GetObjectVariable<LocalVariableInt>($"storagebox{boxIndex}amount");
        amount.Value = (amount.HasValue ? amount.Value : 0) + quantity;

        // Ensure resref and name are set
        jobJournal.GetObjectVariable<LocalVariableString>($"storagebox{boxIndex}").Value = resource.Resref;
        jobJournal.GetObjectVariable<LocalVariableString>($"storagename{boxIndex}").Value = resource.Name;

        return true;
    }

    private bool AddToMiniatureBox(ResourceDataRecord resource, int quantity)
    {
        if (_selectedPlayer?.ControlledCreature == null) return false;

        List<NwItem> boxes = GetMiniatureStorageBoxes(_selectedPlayer);
        int boxIndex = resource.SourceIndex;

        if (boxIndex < 0 || boxIndex >= boxes.Count) return false;

        NwItem box = boxes[boxIndex];
        LocalVariableInt count = box.GetObjectVariable<LocalVariableInt>("storageboxcount");
        count.Value = (count.HasValue ? count.Value : 0) + quantity;

        // Ensure resref and name are set
        box.GetObjectVariable<LocalVariableString>("storagebox").Value = resource.Resref;
        box.GetObjectVariable<LocalVariableString>("storageboxname").Value = resource.Name;
        box.Description = $"Item Count Stored: {count.Value}";
        box.Name = $"<c~Îë>Storage Chest: {resource.Name}</c>";

        return true;
    }

    private bool AddToInventory(ResourceDataRecord resource, int quantity)
    {
        if (_selectedPlayer?.ControlledCreature?.Location == null) return false;

        // Create items one at a time
        for (int i = 0; i < quantity; i++)
        {
            NwItem? created = NwItem.Create(resource.Resref, _selectedPlayer.ControlledCreature.Location);
            if (created == null) return false;

            _selectedPlayer.ControlledCreature.AcquireItem(created);
        }

        return true;
    }

    private NwItem? FindJobJournal(NwPlayer player)
    {
        if (player.ControlledCreature == null) return null;

        foreach (NwItem item in player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == "js_jobjournal")
                return item;
        }
        return null;
    }

    private List<NwItem> GetMiniatureStorageBoxes(NwPlayer player)
    {
        List<NwItem> boxes = new();

        if (player.ControlledCreature == null) return boxes;

        foreach (NwItem item in player.ControlledCreature.Inventory.Items)
        {
            if (item.Tag == "js_merc_2_targ")
                boxes.Add(item);
        }
        return boxes;
    }
}

