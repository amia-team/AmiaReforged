using AmiaReforged.PwEngine.Systems.DungeonMaster.NpcBank;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog.Fluent;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder.EncounterMaker;

public sealed class EncounterMakerPresenter : ScryPresenter<EncounterMakerView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override EncounterMakerView View { get; }
    public override NuiWindowToken Token() => _token;

    private EncounterMakerModel Model { get; }
    
    public delegate void CloseEventHandler(EncounterMakerPresenter? me, EventArgs e);
    
    public event CloseEventHandler? Closing;

    public EncounterMakerPresenter(EncounterMakerView view, NwPlayer player)
    {
        View = view;
        _player = player;

        Model = new(player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Model);
    }

    public override void InitBefore()
    {
        _window = new(View.RootLayout(), View.Title)
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
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                MakeNewEncounter(eventData);
                break;
        }
    }

    private void MakeNewEncounter(ModuleEvents.OnNuiEvent eventData)
    {
        
        if (eventData.ElementId == View.OkButton.Id)
        {
            string? encounterName = Token().GetBindValue(View.Name);

            if (string.IsNullOrEmpty(encounterName))
            {
                _player.SendServerMessage("Name must not be empty.", ColorConstants.Red);
                return;
            }

            Model.EncounterName = encounterName;
            Model.CreateNewEncounter(encounterName);
            Close();
        }
    }

    public override void Close()
    {
        OnClosing(this);
        
        Token().Close();
    }

    private void OnClosing(EncounterMakerPresenter? me)
    {
        Closing?.Invoke(me, EventArgs.Empty);
    }
}