using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.CharacterTools.HeightChanger;

public sealed class HeightChangerPresenter(HeightChangerView view, NwPlayer player)
    : ScryPresenter<HeightChangerView>
{
    public override HeightChangerView View { get; } = view;

    private readonly HeightChangerModel _model = new(player);
    private NuiWindowToken _token;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "Height Changer")
        {
            Geometry = new NuiRect(50f, 50f, 660f, 480f),
            Resizable = false
        };

        if (!player.TryCreateNuiWindow(window, out _token))
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

        // Set target options
        List<NuiComboEntry> options = _model.GetTargetOptions();
        Token().SetBindValue(View.TargetOptions, options);
        Token().SetBindValue(View.TargetSelection, 0);

        // Set default height slider to 0.0
        Token().SetBindValue(View.HeightSlider, 0.0f);
        Token().SetBindValue(View.HeightLabel, "0.0");

        // Initialize the target to the player
        _model.SetSelectedTarget(0);
    }

    private void SetupWatches()
    {
        // Watch for target selection changes
        Token().SetBindWatch(View.TargetSelection, true);

        // Watch for slider changes
        Token().SetBindWatch(View.HeightSlider, true);
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
            case "btn_refresh":
                RefreshTargetList();
                break;

            case "btn_ground":
                SetHeight(0.0f);
                break;

            case "btn_05":
                SetHeight(0.5f);
                break;

            case "btn_10":
                SetHeight(1.0f);
                break;

            case "btn_15":
                SetHeight(1.5f);
                break;

            case "btn_close":
                Close();
                break;
        }
    }

    private void HandleWatchEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == "hc_target_selection")
        {
            int selection = Token().GetBindValue(View.TargetSelection);
            _model.SetSelectedTarget(selection);
        }
        else if (eventData.ElementId == "hc_height_slider")
        {
            float height = Token().GetBindValue(View.HeightSlider);
            Token().SetBindValue(View.HeightLabel, height.ToString("F1"));
            _model.SetHeight(height);
        }
    }

    private void SetHeight(float height)
    {
        Token().SetBindValue(View.HeightSlider, height);
        Token().SetBindValue(View.HeightLabel, height.ToString("F1"));
        _model.SetHeight(height);
    }

    private void RefreshTargetList()
    {
        // Get the current selection
        int currentSelection = Token().GetBindValue(View.TargetSelection);

        // Update the target options
        List<NuiComboEntry> options = _model.GetTargetOptions();
        Token().SetBindValue(View.TargetOptions, options);

        // Try to maintain the current selection if valid, otherwise reset to 0
        if (currentSelection >= options.Count)
        {
            currentSelection = 0;
        }

        Token().SetBindValue(View.TargetSelection, currentSelection);
        _model.SetSelectedTarget(currentSelection);

        player.SendServerMessage("Target list refreshed.", ColorConstants.Cyan);
    }
}

