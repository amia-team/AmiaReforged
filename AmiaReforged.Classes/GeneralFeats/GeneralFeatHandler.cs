using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;


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

    // TODO: Add script handler
    [ScriptHandler(scriptName: "monkey_grip")]
    public void OnMonkeyGrip(CallInfo info)
    {
        NwCreature? playerCharacter = info.ObjectSelf as NwCreature;
        if (playerCharacter is null)
        {
            Log.Info("Could not convert object self to NWCreature");
            return;
        }

        MonkeyGrip mg = new(playerCharacter);
        mg.ChangeSize();
    }
}