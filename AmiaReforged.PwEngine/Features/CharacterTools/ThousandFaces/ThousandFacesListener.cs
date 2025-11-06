using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;

[ServiceBinding(typeof(ThousandFacesListener))]
public class ThousandFacesListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;
    private readonly PlayerNameOverrideService _playerNameOverrideService;

    public ThousandFacesListener(WindowDirector windowDirector, PlayerNameOverrideService playerNameOverrideService)
    {
        _windowDirector = windowDirector;
        _playerNameOverrideService = playerNameOverrideService;
        Log.Info(message: "ThousandFacesListener initialized.");
    }

    [ScriptHandler("i_100faces_init")]
    public void OnItemActivated(CallInfo callInfo)
    {
        Log.Info("One Thousand Faces item script handler triggered");

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

        Log.Info($"Opening One Thousand Faces for player: {player.PlayerName}");

        ThousandFacesView view = new ThousandFacesView(player, _playerNameOverrideService);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening One Thousand Faces...", ColorConstants.Cyan);
    }
}

