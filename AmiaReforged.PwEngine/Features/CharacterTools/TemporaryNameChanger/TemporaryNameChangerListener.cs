using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.TemporaryNameChanger;

[ServiceBinding(typeof(TemporaryNameChangerListener))]
public class TemporaryNameChangerListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;
    private readonly PlayerNameOverrideService _playerNameOverrideService;

    public TemporaryNameChangerListener(WindowDirector windowDirector, PlayerNameOverrideService playerNameOverrideService)
    {
        _windowDirector = windowDirector;
        _playerNameOverrideService = playerNameOverrideService;
        Log.Info(message: "TemporaryNameChangerListener initialized.");
    }

    [ScriptHandler("i_pl_namechanger")]
    public void OnItemActivated(CallInfo callInfo)
    {
        Log.Info("Temporary Name Changer item script handler triggered");

        uint pcObject = NWScript.GetItemActivator();
        NwCreature? creature = pcObject.ToNwObject<NwCreature>();

        if (creature == null || !creature.IsValid)
        {
            Log.Warn("Creature is null or invalid");
            return;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player == null)
        {
            Log.Warn("Player is null");
            return;
        }

        Log.Info($"Opening Temporary Name Changer for player: {player.PlayerName}");

        TemporaryNameChangerView view = new TemporaryNameChangerView(player);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening Temporary Name Changer...", ColorConstants.Cyan);
    }
}

