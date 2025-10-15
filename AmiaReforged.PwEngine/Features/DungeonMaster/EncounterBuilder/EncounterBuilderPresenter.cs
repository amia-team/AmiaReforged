using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.EncounterBuilder;

[UsedImplicitly]
public class EncounterBuilderPresenter : ScryPresenter<EncounterBuilderView>
{
    public override EncounterBuilderView View => _view;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly EncounterBuilderView _view;
    private readonly NwPlayer _player;

    private EncounterBuilderModel Model { get; }

    public EncounterBuilderPresenter(EncounterBuilderView view, NwPlayer player)
    {
        _view = view;
        _player = player;

        Model = new EncounterBuilderModel(_player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Model);
    }

    public override NuiWindowToken Token() => _token;

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

        Token().SetBindValue(View.Selection, 0);

        RefreshEncounters();

        Model.Update += Reload;
    }

    private void Reload(EncounterBuilderModel me, EventArgs e)
    {
        RefreshEncounters();
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
        bool validIndex = eventData.ArrayIndex >= 0
                          && eventData.ArrayIndex <
                          Model.VisibleEncounters.Count;

        if (eventData.ElementId == View.SearchButton.Id)
        {
            RefreshEncounters();
        }
        else if (eventData.ElementId == View.AddEncounterButton.Id)
        {
            Model.OpenEncounterCreator();
        }
        else if (eventData.ElementId == View.SpawnEncounterButton.Id && validIndex)
        {
            int selected = Token().GetBindValue(View.Selection);
            StandardFaction selectedFaction = StandardFaction.Commoner;
            switch (selected)
            {
                case 0:
                    selectedFaction = StandardFaction.Commoner;
                    break;
                case 1:
                    selectedFaction = StandardFaction.Merchant;
                    break;
                case 2:
                    selectedFaction = StandardFaction.Defender;
                    break;
                case 3:
                    selectedFaction = StandardFaction.Hostile;
                    break;
            }

            NwFaction faction = NwFaction.FromStandardFaction(selectedFaction)!;

            Model.PromptSpawn(eventData.ArrayIndex, faction);
        }
        else if (eventData.ElementId == View.EditEncounterButton.Id && validIndex)
        {
            Model.OpenEncounterEditor(eventData.ArrayIndex);
        }
        else if (eventData.ElementId == View.DeleteEncounterButton.Id && validIndex)
        {
            Model.PromptForDeletion(eventData.ArrayIndex);
        }
    }

    private void RefreshEncounters()
    {
        string? searchTerm = Token().GetBindValue(View.Search);

        if (searchTerm != null)
        {
            Model.SetSearchTerm(searchTerm);
        }

        Model.LoadEncounters();
        Model.RefreshEncounters();

        List<string> encounterNames = Model.VisibleEncounters.Select(e => e.Name).ToList();

        Token().SetBindValues(View.EncounterNames, encounterNames);
        Token().SetBindValue(View.EncounterCount, encounterNames.Count);
    }

    public override void Close()
    {
    }
}
