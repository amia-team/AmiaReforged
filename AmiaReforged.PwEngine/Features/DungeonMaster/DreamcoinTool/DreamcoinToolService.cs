using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinTool;

/// <summary>
///   Listens for the DM DC rod activation and opens the Dreamcoin tool window.
/// </summary>
[ServiceBinding(typeof(DreamcoinToolService))]
public class DreamcoinToolService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string DcRodTag = "zol_dc_rod";

    private readonly DreamcoinService _dreamcoinService;
    private readonly PlayerDataService _playerDataService;
    private readonly WindowDirector _director;

    public DreamcoinToolService(DreamcoinService dreamcoinService, PlayerDataService playerDataService,
        WindowDirector director)
    {
        _dreamcoinService = dreamcoinService;
        _playerDataService = playerDataService;
        _director = director;

        NwModule.Instance.OnActivateItem += HandleDcRodActivation;
        Log.Info("DreamcoinToolService initialized.");
    }

    private async void HandleDcRodActivation(ModuleEvents.OnActivateItem obj)
    {
        try
        {
            if (obj.ActivatedItem.Tag != DcRodTag)
                return;

            if (!obj.ItemActivator.IsLoginPlayerCharacter(out NwPlayer? player))
                return;

            // Verify the user is a DM
            bool isDm = await _playerDataService.IsDm(player.CDKey);
            await NwTask.SwitchToMainThread();

            if (!isDm && !player.IsDM)
            {
                player.SendServerMessage("You must be a DM to use the DC rod.");
                return;
            }

            // Get the target player
            NwCreature? targetCreature = obj.TargetObject as NwCreature;
            if (targetCreature == null || !targetCreature.IsPlayerControlled(out NwPlayer? targetPlayer))
            {
                player.SendServerMessage("You must target a player character.");
                return;
            }

            // Open the Dreamcoin tool window
            OpenDreamcoinTool(player, targetPlayer);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in HandleDcRodActivation");
        }
    }

    private void OpenDreamcoinTool(NwPlayer dmPlayer, NwPlayer targetPlayer)
    {
        DreamcoinToolView view = new(dmPlayer, targetPlayer, _dreamcoinService);
        IScryPresenter presenter = view.Presenter;

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            dmPlayer.FloatingTextString("Failed to load the Dreamcoin tool. Report this as a bug.", false);
            return;
        }

        injector.Inject(presenter);
        _director.OpenWindow(presenter);
    }
}
