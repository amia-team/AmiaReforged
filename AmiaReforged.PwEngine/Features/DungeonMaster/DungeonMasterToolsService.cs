using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

/// <summary>
/// Handles the displaying and opening of widgets for the Dungeon Master toolkit
/// </summary>
[ServiceBinding(typeof(DungeonMasterToolsService))]
public class DungeonMasterToolsService(PlayerDataService playerDataService, WindowDirector director)
{
    private readonly PlayerDataService _playerDataService = playerDataService;
    private readonly WindowDirector _director = director;
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
