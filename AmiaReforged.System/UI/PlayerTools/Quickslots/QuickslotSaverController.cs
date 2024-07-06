using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.System.UI.PlayerTools.Quickslots.CreateQuickslots;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.UI.PlayerTools.Quickslots;

public class QuickslotSaverController : WindowController<QuickslotSaverView>
{
    [Inject] private Lazy<QuickslotLoader> QuickslotLoader { get; set; }
    [Inject] private Lazy<WindowManager> WindowManager { get; set; }
    
    [Inject] private Lazy<PlayerDataService> PlayerDataService { get; set; }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private List<SavedQuickslots> _quickslots;
    private List<SavedQuickslots>? _visibleQuickslots = new();

    public override async void Init()
    {
        Guid playerId = PcKeyUtils.GetPcKey(Token.Player);
        
        bool exists = await PlayerDataService.Value.CharacterExists(Token.Player.CDKey, playerId);
        await NwTask.SwitchToMainThread();
        if (!exists)
        {
            Token.Player.SendServerMessage("You need to go in game before you can use this feature.");
            Token.Close();
        }
        
        _quickslots = (await QuickslotLoader.Value.LoadQuickslots(playerId)).ToList();
        await NwTask.SwitchToMainThread();

        RefreshQuickslotList();
    }

    private void RefreshQuickslotList()
    {
        string search = Token.GetBindValue(View.Search)!;
        _visibleQuickslots = _quickslots.Where(q => q.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> quickslotNames = _visibleQuickslots.Select(q => q.Name).ToList();
        List<string> quickslotIds = _visibleQuickslots.Select(q => q.Id.ToString()).ToList();

        Token.SetBindValues(View.QuickslotNames, quickslotNames);
        Token.SetBindValues(View.QuickslotIds, quickslotIds);
        Token.SetBindValue(View.QuickslotCount, quickslotNames.Count);
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
        {
            RefreshQuickslotList();
        }
        else if (eventData.ElementId == View.ViewQuickslotsButton.Id)
        {
            LoadQuickSlot(eventData);
        }
        else if (eventData.ElementId == View.CreateQuickslotsButton.Id)
        {
            OpenQuickslotCreator();
        }
        else if (eventData.ElementId == View.DeleteQuickslotsButton.Id)
        {
            DeleteConfiguration(eventData);
        }
    }

    private void LoadQuickSlot(ModuleEvents.OnNuiEvent eventData)
    {
        SavedQuickslots? selectedQuickslot = _visibleQuickslots?[eventData.ArrayIndex];
        if (selectedQuickslot == null)
        {
            return;
        }

        Token.Player.LoginCreature?.DeserializeQuickbar(selectedQuickslot.Quickslots);
    }

    private void OpenQuickslotCreator()
    {
        WindowManager.Value.OpenWindow<CreateQuickslotsView>(Token.Player);
        Token.Close();
    }

    private async void DeleteConfiguration(ModuleEvents.OnNuiEvent eventData)
    {
        SavedQuickslots? selectedQuickslot = _visibleQuickslots?[eventData.ArrayIndex];
        if (selectedQuickslot == null)
        {
            return;
        }

        await QuickslotLoader.Value.DeleteSavedQuickslot(selectedQuickslot.Id);
        await NwTask.SwitchToMainThread();

        Token.Player.SendServerMessage($"Quickslot configuration {selectedQuickslot.Name} has been deleted.");

        _quickslots.Remove(selectedQuickslot);

        RefreshQuickslotList();
    }

    protected override void OnClose()
    {
        _visibleQuickslots = null;
    }
}