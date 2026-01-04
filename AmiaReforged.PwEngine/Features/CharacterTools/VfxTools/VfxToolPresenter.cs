using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.CharacterTools.VfxTools;

public sealed class VfxToolPresenter : ScryPresenter<VfxToolView>
{
    public override VfxToolView View { get; }

    private readonly VfxToolModel _model;
    private readonly NwPlayer _player;
    private NuiWindowToken _token;

    public VfxToolPresenter(VfxToolView view, NwPlayer player, bool isDm, NwGameObject? selectedTarget)
    {
        View = view;
        _player = player;
        _model = new VfxToolModel(player, isDm, selectedTarget);
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "VFX Tool")
        {
            Geometry = new NuiRect(100f, 100f, View.WindowW, View.WindowH),
            Resizable = true
        };

        if (!_player.TryCreateNuiWindow(window, out _token))
            return;

        InitializeBindValues();
        SetupWatches();
    }

    public override void Close()
    {
        _token.Close();
    }

    private void InitializeBindValues()
    {
        // Enable all controls
        Token().SetBindValue(View.AlwaysEnabled, true);
        Token().SetBindValue(View.IsDmBind, _model.IsDm());

        // Initialize target
        if (!_model.IsDm())
        {
            // For players, find nearest vfx_doll
            _model.FindNearestVfxDoll();
        }

        UpdateTargetDisplay();
        UpdateVfxDisplay();

        // Initialize input fields
        Token().SetBindValue(View.VfxIdInput, _model.GetCurrentVfxId().ToString());

        if (_model.IsDm())
        {
            Token().SetBindValue(View.PermanentVfxInput, "");
            Token().SetBindValue(View.DurationInput, "");
            RefreshActiveVfxList();
        }
    }

    private void SetupWatches()
    {
        // Don't watch VfxIdInput - only update when Apply button is clicked
        // This prevents partial input from being validated while typing

        if (_model.IsDm())
        {
            Token().SetBindWatch(View.SelectedVfxIndex, true);
        }
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClickEvent(eventData);
                break;

            case NuiEventType.Watch:
                HandleWatchEvent(eventData);
                break;
        }
    }

    private void HandleClickEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.ElementId)
        {
            case "btn_previous":
                _model.PreviousVfx();
                UpdateVfxDisplay();
                Token().SetBindValue(View.VfxIdInput, _model.GetCurrentVfxId().ToString());
                // Auto-apply the VFX when cycling
                _model.ApplyVfx(_model.GetCurrentVfxId());
                if (_model.IsDm())
                {
                    RefreshActiveVfxList();
                }
                break;

            case "btn_next":
                _model.NextVfx();
                UpdateVfxDisplay();
                Token().SetBindValue(View.VfxIdInput, _model.GetCurrentVfxId().ToString());
                // Auto-apply the VFX when cycling
                _model.ApplyVfx(_model.GetCurrentVfxId());
                if (_model.IsDm())
                {
                    RefreshActiveVfxList();
                }
                break;

            case "btn_apply":
                ApplyCurrentVfx();
                break;

            case "btn_add_permanent":
                if (_model.IsDm())
                {
                    AddPermanentVfx();
                }
                break;

            case "btn_remove_vfx":
                if (_model.IsDm())
                {
                    RemoveSelectedVfx();
                }
                break;

            case "btn_choose_target":
                if (_model.IsDm())
                {
                    ChooseNewTarget();
                }
                break;

            case "btn_refresh_vfx":
                if (_model.IsDm())
                {
                    RefreshActiveVfxList();
                }
                break;
        }
    }

    private void HandleWatchEvent(ModuleEvents.OnNuiEvent eventData)
    {
        // No watches on input fields - only update on button clicks
        // This prevents partial VFX IDs from being validated while typing
    }

    private void UpdateVfxDisplay()
    {
        Token().SetBindValue(View.CurrentVfxLabel,
            $"VFX {_model.GetCurrentVfxId()}: {_model.GetCurrentVfxLabel()}");
    }

    private void UpdateTargetDisplay()
    {
        Token().SetBindValue(View.TargetNameLabel,
            $"Target: {_model.GetTargetName()}");
    }

    private void ApplyCurrentVfx()
    {
        string input = Token().GetBindValue(View.VfxIdInput) ?? "";
        if (!int.TryParse(input, out int vfxId))
        {
            _player.SendServerMessage("Invalid VFX ID entered.", ColorConstants.Orange);
            return;
        }

        // Update the model's current VFX to match the input
        _model.SetVfxById(vfxId);
        // Update the display label to show the correct VFX info
        UpdateVfxDisplay();
        // Apply the VFX
        _model.ApplyVfx(vfxId);

        if (_model.IsDm())
        {
            // Refresh the active VFX list for DMs
            RefreshActiveVfxList();
        }
    }

    private void AddPermanentVfx()
    {
        string input = Token().GetBindValue(View.PermanentVfxInput) ?? "";
        if (!int.TryParse(input, out int vfxId))
        {
            _player.SendServerMessage("Invalid VFX ID entered.", ColorConstants.Orange);
            return;
        }

        // Check if we're targeting a location
        if (_model.IsLocationTarget())
        {
            // Location targets can only have temporary VFX
            string durationInput = Token().GetBindValue(View.DurationInput) ?? "";
            int duration = 0;
            bool hasDuration = !string.IsNullOrWhiteSpace(durationInput) && int.TryParse(durationInput, out duration);

            if (!hasDuration || duration <= 0)
            {
                _player.SendServerMessage("Location targets require a duration. Please enter a duration in seconds.", ColorConstants.Orange);
                return;
            }

            _model.ApplyVfxWithDuration(vfxId, duration);
            Token().SetBindValue(View.PermanentVfxInput, "");
            Token().SetBindValue(View.DurationInput, "");
            return;
        }

        // For object targets, check for optional duration
        string durationInput2 = Token().GetBindValue(View.DurationInput) ?? "";
        int duration2 = 0;
        bool hasDuration2 = !string.IsNullOrWhiteSpace(durationInput2) && int.TryParse(durationInput2, out duration2);

        if (hasDuration2 && duration2 > 0)
        {
            // Apply temporary VFX with custom duration
            _model.ApplyVfxWithDuration(vfxId, duration2);
            Token().SetBindValue(View.PermanentVfxInput, "");
            Token().SetBindValue(View.DurationInput, "");
            _player.SendServerMessage($"Applied temporary VFX for {duration2} seconds.", ColorConstants.Cyan);
        }
        else
        {
            // Apply permanent VFX
            _model.ApplyVfx(vfxId, true);
            Token().SetBindValue(View.PermanentVfxInput, "");
            Token().SetBindValue(View.DurationInput, "");
        }

        RefreshActiveVfxList();
    }

    private void RemoveSelectedVfx()
    {
        int selectedIndex = Token().GetBindValue(View.SelectedVfxIndex);
        List<VfxEffectInfo> activeVfx = _model.GetActiveVfxList();

        if (selectedIndex < 0 || selectedIndex >= activeVfx.Count)
        {
            _player.SendServerMessage("No VFX selected.", ColorConstants.Orange);
            return;
        }

        VfxEffectInfo selectedVfx = activeVfx[selectedIndex];
        _model.RemoveVfx(selectedVfx.Effect);
        RefreshActiveVfxList();
    }

    private void RefreshActiveVfxList()
    {
        List<VfxEffectInfo> activeVfx = _model.GetActiveVfxList();
        List<NuiComboEntry> entries = new();

        for (int i = 0; i < activeVfx.Count; i++)
        {
            VfxEffectInfo vfx = activeVfx[i];
            entries.Add(new NuiComboEntry($"{vfx.VfxId}: {vfx.Label}", i));
        }

        if (entries.Count == 0)
        {
            entries.Add(new NuiComboEntry("No active VFX", 0));
        }

        Token().SetBindValue(View.ActiveVfxList, entries);
        Token().SetBindValue(View.SelectedVfxIndex, 0);
    }

    private void ChooseNewTarget()
    {
        _player.SendServerMessage("Select a new target for VFX application.", ColorConstants.Cyan);
        _player.EnterTargetMode(OnNewTargetSelected);
    }

    private void OnNewTargetSelected(ModuleEvents.OnPlayerTarget targetEvent)
    {
        // Handle object targets (creatures, placeables, doors, etc.)
        if (targetEvent.TargetObject != null && targetEvent.TargetObject.IsValid)
        {
            if (targetEvent.TargetObject is NwGameObject gameObject)
            {
                _model.SetTarget(gameObject);
                UpdateTargetDisplay();
                RefreshActiveVfxList();
                _player.SendServerMessage($"Target changed to: {gameObject.Name}", ColorConstants.Cyan);
                return;
            }
        }

        // Handle ground/location targets - set as location target directly
        if (_player.ControlledCreature?.Area != null)
        {
            Location targetLocation = Location.Create(
                _player.ControlledCreature.Area,
                targetEvent.TargetPosition,
                0f);

            _model.SetTargetLocation(targetLocation);
            UpdateTargetDisplay();

            // For location targets, we can't list active VFX, so show a message
            Token().SetBindValue(View.ActiveVfxList, new List<NuiComboEntry>
            {
                new NuiComboEntry("Location targets cannot list VFX", 0)
            });
            Token().SetBindValue(View.SelectedVfxIndex, 0);

            _player.SendServerMessage($"Target set to ground location (VFX will be temporary only)", ColorConstants.Cyan);
            return;
        }

        _player.SendServerMessage("Invalid target selected.", ColorConstants.Orange);
    }
}

