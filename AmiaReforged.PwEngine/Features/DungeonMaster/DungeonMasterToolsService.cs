using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

/// <summary>
/// Handles the displaying and opening of widgets for the Dungeon Master toolkit
/// </summary>
[ServiceBinding(typeof(DungeonMasterToolsService))]
public class DungeonMasterToolsService
{
    private readonly PlayerDataService _playerDataService;
    private readonly WindowDirector _director;

    public DungeonMasterToolsService(PlayerDataService playerDataService, WindowDirector director)
    {
        _playerDataService = playerDataService;
        _director = director;
        NwModule.Instance.OnPlayerRest += OnDmRest;
    }

    private void OnDmRest(ModuleEvents.OnPlayerRest obj)
    {
        // Only trigger on rest started
        if (obj.RestEventType != RestEventType.Started) return;

        // Only for DMs
        if (!obj.Player.IsDM) return;

        // Cancel the rest action
        obj.Player.LoginCreature?.ClearActionQueue();

        // Open DM tools instead
        _ = OpenToolkit(obj.Player);
    }
    public async Task OpenToolkit(NwPlayer player)
    {
        bool isDm = await _playerDataService.IsDm(player.CDKey);

        await NwTask.SwitchToMainThread();

        bool notOnDmClient = !player.IsDM;
        if (!isDm && notOnDmClient)
        {
            player.SendServerMessage("You are not a DM, so you cannot access the DM toolkit");
            return;
        }

        IScryPresenter presenter = DmWindowFactory.OpenDmTools(player);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            player.FloatingTextString(
                message:
                "Failed to load the player tools due to missing DI container. Screenshot this and report it as a bug.",
                false);
            return;
        }

        injector.Inject(presenter);
        _director.OpenWindow(presenter);
    }
}
