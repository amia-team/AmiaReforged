using AmiaReforged.PwEngine.Systems.Crafting;
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
}
