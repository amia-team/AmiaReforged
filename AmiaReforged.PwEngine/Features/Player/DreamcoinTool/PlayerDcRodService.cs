using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

/// <summary>
///   Listens for the player DC rod activation and opens the appropriate window.
/// </summary>
[ServiceBinding(typeof(PlayerDcRodService))]
public class PlayerDcRodService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string PcDcRodTag = "pc_dcrod";

    private readonly DreamcoinService _dreamcoinService;
    private readonly DcPlaytimeService _playtimeService;
    private readonly WindowDirector _director;

    public PlayerDcRodService(DreamcoinService dreamcoinService, DcPlaytimeService playtimeService, WindowDirector director)
    {
        _dreamcoinService = dreamcoinService;
        _playtimeService = playtimeService;
        _director = director;

        NwModule.Instance.OnActivateItem += HandlePcDcRodActivation;
        Log.Info("PlayerDcRodService initialized.");
    }

    private void HandlePcDcRodActivation(ModuleEvents.OnActivateItem obj)
    {
        if (obj.ActivatedItem.Tag != PcDcRodTag)
            return;

        if (!obj.ItemActivator.IsLoginPlayerCharacter(out NwPlayer? player))
            return;

        NwCreature? targetCreature = obj.TargetObject as NwCreature;

        // Self-target: show balance and burn DC window
        if (targetCreature == null || targetCreature == obj.ItemActivator)
        {
            OpenSelfWindow(player);
            return;
        }

        // Other player target: show donate/recommend window
        if (targetCreature.IsPlayerControlled(out NwPlayer? targetPlayer))
        {
            OpenDonateWindow(player, targetPlayer);
        }
        else
        {
            player.SendServerMessage("You must target yourself or another player.");
        }
    }

    private void OpenSelfWindow(NwPlayer player)
    {
        PlayerDcSelfView view = new(player, _dreamcoinService, _playtimeService);
        IScryPresenter presenter = view.Presenter;

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            player.FloatingTextString("Failed to load the DC tool. Report this as a bug.", false);
            return;
        }

        injector.Inject(presenter);
        _director.OpenWindow(presenter);
    }

    private void OpenDonateWindow(NwPlayer player, NwPlayer targetPlayer)
    {
        PlayerDcDonateView view = new(player, targetPlayer, _dreamcoinService);
        IScryPresenter presenter = view.Presenter;

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            player.FloatingTextString("Failed to load the DC tool. Report this as a bug.", false);
            return;
        }

        injector.Inject(presenter);
        _director.OpenWindow(presenter);
    }
}
