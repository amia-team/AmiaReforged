using AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;
using AmiaReforged.PwEngine.Features.Crafting;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;


namespace AmiaReforged.Classes.GeneralFeats;

[ServiceBinding(typeof(GeneralFeatHandler))]
public class GeneralFeatHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GeneralFeatHandler()
    {
        NwModule.Instance.OnClientEnter += ReapplyMonkeyGrip;
    }

    /// <summary>
    /// Relogging apparently loses the changed creature size
    /// </summary>
    private void ReapplyMonkeyGrip(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature is not {} loginCreature) return;

        MonkeyGrip mg = new(loginCreature);

        if (!mg.IsMonkeyGripped()) return;

        mg.ApplyMonkeyGrip();
    }

    [ScriptHandler(scriptName: "monkey_grip")]
    public void OnMonkeyGrip(CallInfo info)
    {
        NwCreature? gameCharacter = info.ObjectSelf as NwCreature;
        if (gameCharacter is null)
        {
            Log.Info("Could not convert object self to NWCreature");
            return;
        }

        NwItem? mainHand = gameCharacter.GetItemInSlot(InventorySlot.RightHand);

        if (mainHand == null) return;

        bool isCasterWeapon = NWScript.GetLocalInt(mainHand, CasterWeaponForge.LocalIntCasterWeapon) == NWScript.TRUE;

        if (isCasterWeapon)
        {
            NWScript.SendMessageToPC(gameCharacter, "You cannot monkey grip a caster weapon!");
            return;
        }

        MonkeyGrip mg = new(gameCharacter);
        mg.ApplyMonkeyGrip();
    }

    [ScriptHandler(scriptName: "ft_1000faces")]
    public void OnThousandFaces(CallInfo info)
    {
        NwCreature? creature = info.ObjectSelf as NwCreature;
        if (creature is null)
        {
            return;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player == null)
        {
            return;
        }

        // Get services needed to open the window
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        PlayerNameOverrideService? nameService = AnvilCore.GetService<PlayerNameOverrideService>();
        InjectionService? injector = AnvilCore.GetService<InjectionService>();

        if (windowDirector == null || nameService == null)
        {
            player.SendServerMessage("Error: Could not open One Thousand Faces window.", ColorConstants.Red);
            return;
        }

        ThousandFacesView view = new ThousandFacesView(player, nameService);

        if (injector != null)
        {
            injector.Inject(view.Presenter);
        }

        windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening One Thousand Faces...", ColorConstants.Cyan);
    }
}
