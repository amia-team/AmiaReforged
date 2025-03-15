using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots.CreateQuickslots;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Quickslots;

public class QuickslotSaverPresenter : ScryPresenter<QuickslotSaverView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwPlayer _player;

    private List<SavedQuickslots> _quickslots;

    private NuiWindowToken _token;
    private List<SavedQuickslots>? _visibleQuickslots = new();
    private NuiWindow? _window;

    public QuickslotSaverPresenter(QuickslotSaverView toolView, NwPlayer player)
    {
        _player = player;
        View = toolView;
    }

    [Inject] private Lazy<QuickslotLoader> QuickslotLoader { get; set; }
    [Inject] private Lazy<WindowDirector> WindowDirector { get; set; }

    [Inject] private Lazy<PlayerDataService> PlayerDataService { get; set; }

    public override QuickslotSaverView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override async void Create()
    {
        // Create the window if it's null.
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        try
        {
            Guid playerId = PcKeyUtils.GetPcKey(Token().Player);

            bool exists = await PlayerDataService.Value.CharacterExists(Token().Player.CDKey, playerId);
            await NwTask.SwitchToMainThread();
            if (!exists)
            {
                Token().Player.SendServerMessage(message: "You need to go in game before you can use this feature.");
                Token().Close();
            }

            _quickslots = (await QuickslotLoader.Value.LoadQuickslots(playerId)).ToList();
            await NwTask.SwitchToMainThread();

            Token().SetBindValue(View.Search, string.Empty);
            RefreshQuickslotList();
        }
        catch (Exception e)
        {
            Log.Info("Error loading quickslots: " + e.Message);
        }
    }


    private void RefreshQuickslotList()
    {
        string search = Token().GetBindValue(View.Search)!;
        _visibleQuickslots = _quickslots.Where(q => q.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> quickslotNames = _visibleQuickslots.Select(q => q.Name).ToList();
        List<string> quickslotIds = _visibleQuickslots.Select(q => q.Id.ToString()).ToList();

        Token().SetBindValues(View.QuickslotNames, quickslotNames);
        Token().SetBindValues(View.QuickslotIds, quickslotIds);
        Token().SetBindValue(View.QuickslotCount, quickslotNames.Count);
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
        if (eventData.ElementId == View.SearchButton.Id)
            RefreshQuickslotList();
        else if (eventData.ElementId == View.ViewQuickslotsButton.Id)
            LoadQuickSlot(eventData);
        else if (eventData.ElementId == View.CreateQuickslotsButton.Id)
            OpenQuickslotCreator();
        else if (eventData.ElementId == View.DeleteQuickslotsButton.Id) DeleteConfiguration(eventData);
    }

    private void LoadQuickSlot(ModuleEvents.OnNuiEvent eventData)
    {
        SavedQuickslots? selectedQuickslot = _visibleQuickslots?[eventData.ArrayIndex];
        if (selectedQuickslot == null) return;

        Token().Player.LoginCreature?.DeserializeQuickbar(selectedQuickslot.Quickslots);
    }

    private void OpenQuickslotCreator()
    {
        // We're okay with an ad hoc use of the injection service for now
        InjectionService? injectionService = AnvilCore.GetService<InjectionService>();
        if (injectionService is null) return;
        CreateQuickslotsView view = new(_token.Player);
        CreateQuickSlotsPresenter presenter = view.Presenter;

        injectionService.Inject(presenter);

        WindowDirector.Value.OpenWindow(presenter);
        Token().Close();
    }

    private async void DeleteConfiguration(ModuleEvents.OnNuiEvent eventData)
    {
        SavedQuickslots? selectedQuickslot = _visibleQuickslots?[eventData.ArrayIndex];
        if (selectedQuickslot == null) return;

        await QuickslotLoader.Value.DeleteSavedQuickslot(selectedQuickslot.Id);
        await NwTask.SwitchToMainThread();

        Token().Player.SendServerMessage($"Quickslot configuration {selectedQuickslot.Name} has been deleted.");

        _quickslots.Remove(selectedQuickslot);

        RefreshQuickslotList();
    }

    public override void Close()
    {
        _visibleQuickslots = null;
    }
}