using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.CopyMachine;

public sealed class CopyMachinePresenter : ScryPresenter<CopyMachineView>
{
    public override CopyMachineView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    private readonly CopyMachineModel _model;

    public CopyMachinePresenter(CopyMachineView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new CopyMachineModel(player);
        _model.OnSelectionChanged += RefreshStatus;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 250f, 250f)
        };
    }

    public override void Create()
    {
        if (_window == null)
            InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        Token().SetBindValue(View.StatusText, _model.GetStatusText());
        Token().SetBindValue(View.CopyEquipmentChecked, false); // Reset checkbox when source changes
        Token().SetBindValue(View.TypeTooltip, _model.GetTypeString());
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.SelectSourceButton.Id)
        {
            _model.EnterSourceTargetingMode();
        }
        else if (eventData.ElementId == View.CopyToTargetButton.Id)
        {
            // Read checkbox state before entering copy mode
            bool isChecked = Token().GetBindValue(View.CopyEquipmentChecked);
            _model.SetCopyEquipmentFlag(isChecked);
            _model.EnterCopyTargetingMode();
        }
        else if (eventData.ElementId == View.CopyEquipmentCheckbox.Id)
        {
            // Handle checkbox click
            bool isChecked = Token().GetBindValue(View.CopyEquipmentChecked);
            _model.SetCopyEquipmentFlag(isChecked);
        }
    }

    public override void Close()
    {
    }
}

