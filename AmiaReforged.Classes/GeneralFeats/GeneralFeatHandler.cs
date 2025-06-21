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
        NwModule.Instance.OnClientEnter += HandleFeatEffects;
    }

    private void HandleFeatEffects(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature == null) return;
        if (!obj.Player.LoginCreature.IsValid) return;
        
        MonkeyGrip mg = new(obj.Player.LoginCreature);

        if (mg.IsMonkeyGripped())
        {
            mg.ApplyMgPenalty();
        }
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
        mg.ChangeSize();
    }
}