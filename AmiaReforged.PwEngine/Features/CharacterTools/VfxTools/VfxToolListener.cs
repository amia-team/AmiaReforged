using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterTools.VfxTools;

[ServiceBinding(typeof(VfxToolListener))]
public class VfxToolListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;

    public VfxToolListener(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
        Log.Info(message: "VfxToolListener initialized.");
    }

    [ScriptHandler("i_vfx_tool")]
    public void OnScriptCalled(CallInfo callInfo)
    {
        Log.Info("VFX Tool script handler triggered");

        // Get the player who used the object (works for both placeable OnUse and item activation)
        uint pcObject = NWScript.GetLastUsedBy();

        // If GetLastUsedBy returns INVALID (e.g., when called via item), fall back to GetItemActivator
        if (pcObject == NWScript.OBJECT_INVALID)
        {
            pcObject = NWScript.GetItemActivator();
        }

        // If still invalid, try OBJECT_SELF (direct script execution)
        if (pcObject == NWScript.OBJECT_INVALID)
        {
            pcObject = NWScript.OBJECT_SELF;
        }

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

        Log.Info($"Opening VFX Tool for player: {player.PlayerName}");

        // Check if DM
        bool isDm = player.IsDM;
        NwGameObject? selectedTarget = null;

        if (isDm)
        {
            // For DMs using items, check selected target
            uint activatedTarget = NWScript.GetItemActivatedTarget();
            if (activatedTarget != NWScript.OBJECT_INVALID)
            {
                selectedTarget = activatedTarget.ToNwObject<NwGameObject>();
            }
        }

        VfxToolView view = new VfxToolView(player, isDm, selectedTarget);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening VFX Tool...", ColorConstants.Cyan);
    }
}

