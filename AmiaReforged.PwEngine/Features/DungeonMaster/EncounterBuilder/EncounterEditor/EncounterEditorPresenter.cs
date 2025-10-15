using AmiaReforged.Core.Models;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.EncounterBuilder.EncounterEditor;

public sealed class EncounterEditorPresenter : ScryPresenter<EncounterEditorView>
{
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;
    public override EncounterEditorView View { get; }
    public override NuiWindowToken Token() => _token;



    private EncounterEditorModel Model { get; }

    public delegate void WindowClosing(EncounterEditorPresenter me, EventArgs e);

    public event WindowClosing? OnClosing;

    public EncounterEditorPresenter(EncounterEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;

        Model = new EncounterEditorModel(player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Model);
    }

    public void SetModelEncounter(Encounter e)
    {
        Model.Encounter = e;
    }
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override void Create()
    {
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

        Model.EntryUpdated += UpdateList;
        RefreshEntries();
    }

    private void UpdateList()
    {
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        string search = Token().GetBindValue(View.Search)!;
        Model.SetSearchTerm(search);
        Model.LoadEntries();
        Model.RefreshEntryList();

        List<string> entryNames = Model.VisibleEntries.Select(e => e.Name).ToList();

        Token().SetBindValue(View.EntryCount, Model.VisibleEntries.Count);
        Token().SetBindValues(View.EntryNames, entryNames);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonPress(eventData);
                break;
        }
    }

    private void HandleButtonPress(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.AddNpcButton.Id)
        {
            Model.PromptAdd();
            return;
        }

        bool isValidIndex = eventData.ArrayIndex >= 0
                            && eventData.ArrayIndex < Model.VisibleEntries.Count;

        if (eventData.ElementId == View.DeleteNpcButton.Id && isValidIndex)
        {
            Model.PromptDelete(eventData.ArrayIndex);
        }
        else if (eventData.ElementId == View.AddNpcButton.Id && isValidIndex)
        {
            Model.PromptAdd();
        }
    }

    public override void Close()
    {
        OnOnClosing(this);
        Token().Close();
    }

    private void OnOnClosing(EncounterEditorPresenter me)
    {
        OnClosing?.Invoke(me, EventArgs.Empty);
    }
}
