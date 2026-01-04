using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.JobSystemResourceManager;

public sealed class JobResourceManagerPresenter : ScryPresenter<JobResourceManagerView>
{
    public override JobResourceManagerView View { get; }

    private readonly NwPlayer _player;
    private readonly JobResourceManagerModel _model;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private List<ResourceDataRecord> _currentResources = new();
    private int _selectedResourceIndex = -1;

    private bool _processingEvent;

    public override NuiWindowToken Token() => _token;

    public JobResourceManagerPresenter(JobResourceManagerView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new JobResourceManagerModel(player);
    }

    public override void Create()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(300f, 100f, 660f, 675f),
            Resizable = true,
        };

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        // Initialize and load resources
        LoadResources();
    }

    public override void InitBefore()
    {
    }

    public override void Close()
    {
        Token().Close();
    }

    private void LoadResources()
    {
        _currentResources = _model.LoadAllResources();
        UpdateResourceListDisplay();
    }

    private void UpdateResourceListDisplay()
    {
        List<string> names = new();
        List<string> quantities = new();
        List<string> sources = new();
        List<string> transferQuantities = new();

        foreach (ResourceDataRecord resource in _currentResources)
        {
            names.Add(resource.Name);
            quantities.Add(resource.Quantity.ToString());
            sources.Add(GetSourceDisplayName(resource.Source));
            transferQuantities.Add("1"); // Default transfer quantity
        }

        Token().SetBindValue(View.ResourceCount, _currentResources.Count);
        Token().SetBindValues(View.ResourceNames, names);
        Token().SetBindValues(View.ResourceQuantities, quantities);
        Token().SetBindValues(View.ResourceSources, sources);
        Token().SetBindValues(View.TransferQuantities, transferQuantities);
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
        // This gives NWN engine time to process inventory changes (item.Destroy(), etc.)
        NWScript.DelayCommand(2.0f, () =>
        {
            // Check if player is still valid before refreshing
            if (_player?.IsValid == true && _player.ControlledCreature != null)
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
            if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.Open)
                return;

            // Main window events
            if (ev.ElementId == View.RefreshButton.Id)
            {
                LoadResources();
                _player.SendServerMessage("Resources refreshed.", new Color(0, 255, 0));
                return;
            }

            if (ev.ElementId == View.CloseButton.Id)
            {
                Close();
                return;
            }

            // Transfer button - enter targeting mode
            if (ev.ElementId == "btn_transfer")
            {
                _selectedResourceIndex = ev.ArrayIndex;

                if (_selectedResourceIndex < 0 || _selectedResourceIndex >= _currentResources.Count)
                    return;

                ResourceDataRecord resource = _currentResources[_selectedResourceIndex];

                // Get transfer quantity
                List<string> quantities = Token().GetBindValues(View.TransferQuantities);
                if (_selectedResourceIndex >= quantities.Count)
                    return;

                if (!int.TryParse(quantities[_selectedResourceIndex], out int quantity) || quantity <= 0)
                {
                    _player.SendServerMessage("Invalid transfer quantity.", new Color(255, 0, 0));
                    return;
                }

                if (quantity > resource.Quantity)
                {
                    _player.SendServerMessage($"Cannot transfer more than available ({resource.Quantity}).", new Color(255, 0, 0));
                    return;
                }

                // Enter targeting mode
                _player.SendServerMessage("Select transfer destination: yourself for Merchant box, another player, or a miniature storage box item.", new Color(0, 255, 255));
                _player.EnterTargetMode(OnTransferTargetSelected, new TargetModeSettings
                {
                    ValidTargets = ObjectTypes.Creature | ObjectTypes.Item
                });
                return;
            }

            // Move to inventory button
            if (ev.ElementId == "btn_to_inventory")
            {
                if (ev.ArrayIndex < 0 || ev.ArrayIndex >= _currentResources.Count)
                    return;

                ResourceDataRecord resource = _currentResources[ev.ArrayIndex];

                // Get the transfer quantity
                List<string> quantities = Token().GetBindValues(View.TransferQuantities);

                if (ev.ArrayIndex >= quantities.Count)
                    return;

                if (!int.TryParse(quantities[ev.ArrayIndex], out int quantity) || quantity <= 0)
                {
                    _player.SendServerMessage("Invalid transfer quantity.", new Color(255, 0, 0));
                    return;
                }

                // Safeguard: Maximum 100 items at a time to prevent inventory overflow
                if (quantity > 100)
                {
                    _player.SendServerMessage("Cannot transfer more than 100 items to inventory at once (to prevent overflow).", new Color(255, 165, 0));
                    return;
                }

                if (quantity > resource.Quantity)
                {
                    _player.SendServerMessage($"Cannot transfer more than available ({resource.Quantity}).", new Color(255, 0, 0));
                    return;
                }

                // Transfer to inventory
                if (_model.TransferResource(resource, quantity, ResourceTransferDestination.Inventory))
                {
                    _player.SendServerMessage($"Transferred {quantity} {resource.Name} to inventory.", new Color(0, 255, 0));
                    DelayedRefresh();
                }
                else
                {
                    _player.SendServerMessage("Transfer failed.", new Color(255, 0, 0));
                }
                return;
            }

        }
        finally
        {
            _processingEvent = false;
        }
    }

    private void OnTransferTargetSelected(ModuleEvents.OnPlayerTarget targetEvent)
    {
        if (_selectedResourceIndex < 0 || _selectedResourceIndex >= _currentResources.Count)
            return;

        ResourceDataRecord resource = _currentResources[_selectedResourceIndex];

        // Get transfer quantity
        List<string> quantities = Token().GetBindValues(View.TransferQuantities);
        if (_selectedResourceIndex >= quantities.Count)
            return;

        if (!int.TryParse(quantities[_selectedResourceIndex], out int quantity) || quantity <= 0)
        {
            _player.SendServerMessage("Invalid transfer quantity.", new Color(255, 0, 0));
            return;
        }

        if (quantity > resource.Quantity)
        {
            _player.SendServerMessage($"Cannot transfer more than available ({resource.Quantity}).", new Color(255, 0, 0));
            return;
        }

        // Case 1: Targeting an item (miniature storage box)
        if (targetEvent.TargetObject is NwItem targetItem)
        {
            HandleMiniatureBoxTransfer(resource, quantity, targetItem);
            return;
        }

        // Case 2: Targeting a creature (self or other player)
        if (targetEvent.TargetObject is NwCreature targetCreature)
        {
            if (targetCreature.ControllingPlayer == null)
            {
                _player.SendServerMessage("Target must be a player character.", new Color(255, 0, 0));
                return;
            }

            NwPlayer targetPlayer = targetCreature.ControllingPlayer;

            // Case 2a: Self-targeting
            if (targetPlayer == _player)
            {
                HandleSelfMerchantTransfer(resource, quantity);
                return;
            }

            // Case 2b: Other player
            HandleOtherPlayerTransfer(resource, quantity, targetPlayer);
            return;
        }

        _player.SendServerMessage("Invalid target selected.", new Color(255, 0, 0));
    }

    private void HandleSelfMerchantTransfer(ResourceDataRecord resource, int quantity)
    {
        // Check if already in own merchant box
        if (resource.Source == ResourceSource.MerchantBox)
        {
            _player.SendServerMessage("This resource is already in your Merchant box.", new Color(255, 165, 0));
            return;
        }

        if (!_model.IsMerchantJob())
        {
            _player.SendServerMessage("You must have the Merchant job to use Merchant storage.", new Color(255, 0, 0));
            return;
        }

        int availableSlots = _model.GetAvailableMerchantBoxSlots();

        // Check if merchant box already has this resource
        bool hasExistingBox = _model.HasMerchantBoxWithResource(resource.Resref);

        if (availableSlots <= 0 && !hasExistingBox)
        {
            _player.SendServerMessage("No available Merchant storage slots.", new Color(255, 0, 0));
            return;
        }

        if (_model.TransferResource(resource, quantity, ResourceTransferDestination.SelfMerchantBox))
        {
            _player.SendServerMessage($"Transferred {quantity} {resource.Name} to your Merchant storage.", new Color(0, 255, 0));
            _selectedResourceIndex = -1; // Reset selection after transfer
            DelayedRefresh();
        }
        else
        {
            _player.SendServerMessage("Transfer failed.", new Color(255, 0, 0));
        }
    }

    private void HandleOtherPlayerTransfer(ResourceDataRecord resource, int quantity, NwPlayer targetPlayer)
    {
        // Check if target has job journal
        NwItem? targetJournal = FindJobJournal(targetPlayer);
        if (targetJournal == null)
        {
            _player.SendServerMessage($"{targetPlayer.PlayerName} does not have a job journal.", new Color(255, 0, 0));
            return;
        }

        // Check if target is a merchant
        if (!IsMerchant(targetJournal))
        {
            _player.SendServerMessage($"{targetPlayer.PlayerName} does not have the Merchant job.", new Color(255, 0, 0));
            return;
        }

        // Check if target has available merchant box slots or existing box with this resource
        bool hasExistingBox = HasMerchantBoxWithResource(targetJournal, resource.Resref);
        int availableSlots = GetAvailableMerchantBoxSlots(targetJournal);

        if (availableSlots <= 0 && !hasExistingBox)
        {
            _player.SendServerMessage($"{targetPlayer.PlayerName} has no available Merchant storage slots for this resource.", new Color(255, 0, 0));
            return;
        }

        if (_model.TransferResource(resource, quantity, ResourceTransferDestination.OtherPlayerMerchantBox, targetPlayer))
        {
            int boxIndex = _model.GetLastOtherPlayerBoxIndex();
            _player.SendServerMessage($"Transferred {quantity} {resource.Name} to {targetPlayer.PlayerName}'s Merchant storage.", new Color(0, 255, 0));

            if (boxIndex > 0)
            {
                targetPlayer.SendServerMessage($"{_player.PlayerName} has transferred {quantity} {resource.Name} to you. It has been added to Merchant Box #{boxIndex}.", new Color(0, 255, 0));
            }
            else
            {
                targetPlayer.SendServerMessage($"{_player.PlayerName} transferred {quantity} {resource.Name} to your Merchant storage.", new Color(0, 255, 0));
            }

            _selectedResourceIndex = -1; // Reset selection after transfer
            DelayedRefresh();
        }
        else
        {
            _player.SendServerMessage("Transfer failed.", new Color(255, 0, 0));
        }
    }

    private void HandleMiniatureBoxTransfer(ResourceDataRecord resource, int quantity, NwItem targetItem)
    {
        if (targetItem.Tag != "js_merc_2_targ")
        {
            _player.SendServerMessage("Target must be a miniature storage box.", new Color(255, 0, 0));
            return;
        }

        // Check if box belongs to player
        if (targetItem.Possessor != _player.ControlledCreature)
        {
            _player.SendServerMessage("The miniature storage box must be in your inventory.", new Color(255, 0, 0));
            return;
        }

        LocalVariableString boxResref = targetItem.GetObjectVariable<LocalVariableString>("storagebox");
        LocalVariableInt boxCount = targetItem.GetObjectVariable<LocalVariableInt>("storageboxcount");

        // Check if box is empty or has same resource
        if (boxResref.HasValue && boxResref.Value != "" && boxResref.Value != resource.Resref)
        {
            _player.SendServerMessage($"This miniature storage box already contains a different resource ({boxResref.Value}).", new Color(255, 0, 0));
            return;
        }

        // Find the index of this miniature box
        List<NwItem> miniatureBoxes = _model.GetMiniatureStorageBoxes();
        int boxIndex = -1;
        for (int i = 0; i < miniatureBoxes.Count; i++)
        {
            if (miniatureBoxes[i] == targetItem)
            {
                boxIndex = i;
                break;
            }
        }

        if (boxIndex < 0)
        {
            _player.SendServerMessage("Could not find the selected miniature storage box.", new Color(255, 0, 0));
            return;
        }

        if (_model.TransferResource(resource, quantity, ResourceTransferDestination.MiniatureBox, null, boxIndex))
        {
            _player.SendServerMessage($"Transferred {quantity} {resource.Name} to the miniature storage box.", new Color(0, 255, 0));
            _selectedResourceIndex = -1; // Reset selection after transfer
            DelayedRefresh();
        }
        else
        {
            _player.SendServerMessage("Transfer failed. The box may already contain a different resource.", new Color(255, 0, 0));
        }
    }

    // Helper methods for checking merchant job status on other players
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

    private bool IsMerchant(NwItem journal)
    {
        LocalVariableString primary = journal.GetObjectVariable<LocalVariableString>("primaryjob");
        LocalVariableString secondary = journal.GetObjectVariable<LocalVariableString>("secondaryjob");

        return (primary.HasValue && primary.Value == "Merchant") ||
               (secondary.HasValue && secondary.Value == "Merchant");
    }

    private bool HasMerchantBoxWithResource(NwItem journal, string resref)
    {
        // Determine max boxes based on merchant type
        LocalVariableString primary = journal.GetObjectVariable<LocalVariableString>("primaryjob");
        int maxBoxes = (primary.HasValue && primary.Value == "Merchant") ? 30 : 10;

        for (int i = 1; i <= maxBoxes; i++)
        {
            LocalVariableString boxResref = journal.GetObjectVariable<LocalVariableString>($"storagebox{i}");
            if (boxResref.HasValue && boxResref.Value == resref)
            {
                return true;
            }
        }
        return false;
    }

    private int GetAvailableMerchantBoxSlots(NwItem journal)
    {
        // Determine max boxes based on merchant type
        LocalVariableString primary = journal.GetObjectVariable<LocalVariableString>("primaryjob");
        int maxBoxes = (primary.HasValue && primary.Value == "Merchant") ? 30 : 10;

        int usedSlots = 0;
        for (int i = 1; i <= maxBoxes; i++)
        {
            LocalVariableString resref = journal.GetObjectVariable<LocalVariableString>($"storagebox{i}");
            if (resref.HasValue && resref.Value != "")
                usedSlots++;
        }
        return maxBoxes - usedSlots;
    }
}

