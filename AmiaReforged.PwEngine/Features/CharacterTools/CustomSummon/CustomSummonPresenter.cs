using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class CustomSummonPresenter(CustomSummonView view, NwPlayer player, NwItem widget)
    : ScryPresenter<CustomSummonView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public override CustomSummonView View { get; } = view;

    private readonly CustomSummonModel _model = new(player, widget);
    private NuiWindowToken _token;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "Custom Summon Selection")
        {
            Geometry = new NuiRect(0f, 50f, 630f, 570f),
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

        // Get summon data from model
        List<string> summonNames = _model.GetSummonNames();
        int currentSelection = _model.GetCurrentSelection();

        // Set bind values
        Token().SetBindValue(View.SummonCount, summonNames.Count);
        Token().SetBindValues(View.SummonNames, summonNames);
        Token().SetBindValue(View.SelectedSummonIndex, currentSelection);
    }

    private void SetupWatches()
    {
        // Watch for selection changes
        Token().SetBindWatch(View.SelectedSummonIndex, true);
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
            case "cs_btn_select_summon":
                // User clicked on a summon in the list
                int selectedIndex = eventData.ArrayIndex;
                Log.Info($"Summon list item clicked, index: {selectedIndex}");
                Token().SetBindValue(View.SelectedSummonIndex, selectedIndex);
                _model.SetTemporarySelection(selectedIndex);
                break;

            case "cs_btn_confirm":
                ConfirmSelection();
                break;

            case "cs_btn_close":
                Close();
                break;
        }
    }

    private void HandleWatchEvent(ModuleEvents.OnNuiEvent eventData)
    {
        Log.Info($"Watch event triggered for element: {eventData.ElementId}");

        if (eventData.ElementId == "cs_selected_summon")
        {
            // Selection changed in the list
            int selection = Token().GetBindValue(View.SelectedSummonIndex);
            Log.Info($"List selection changed to: {selection}");
            _model.SetTemporarySelection(selection);
        }
    }

    private void ConfirmSelection()
    {
        int selection = Token().GetBindValue(View.SelectedSummonIndex);
        Log.Info($"ConfirmSelection called with selection: {selection}");

        _model.SetCurrentSelection(selection);

        string selectedName = _model.GetSelectedSummonName();
        Log.Info($"Confirmed selection, sending message: Set to {selectedName}");

        player.SendServerMessage($"Set to {selectedName.ColorString(ColorConstants.Cyan)}.", ColorConstants.Green);

        Close();
    }
}

