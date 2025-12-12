using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.HeightChanger;

[ServiceBinding(typeof(HeightChangerListener))]
public class HeightChangerListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;

    public HeightChangerListener(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
        Log.Info(message: "HeightChangerListener initialized.");
    }

    [ScriptHandler("i_axis_changer")]
    public void OnItemActivated(CallInfo callInfo)
    {
        Log.Info("Height Changer item script handler triggered");

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

        Log.Info($"Opening Height Changer for player: {player.PlayerName}");

        HeightChangerView view = new HeightChangerView(player);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening Height Changer...", ColorConstants.Cyan);
    }
}

