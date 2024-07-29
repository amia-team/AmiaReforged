using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Services.Defender;

[ServiceBinding(typeof(DefenderScriptHandlers))]
public class DefenderScriptHandlers
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private IDictionary<NwObject, DefendersDuty> ActiveDuties { get; set; }

    private const string FriendsOnly = "This ability can only be used on friendly creatures.";
    private const float DefenderDamage = 0.25f;


    public DefenderScriptHandlers()
    {
        Log.Info("Setup Defender Script Handlers.");
        ActiveDuties = new Dictionary<NwObject, DefendersDuty>();
    }


    [ScriptHandler("todo_replace_me")]
    public void OnDefendersDuty(CallInfo info)
    {
        if (info.ObjectSelf == null)
        {
            Log.Warn("Defenders Duty called with no object self.");
            return;
        }

        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        NwObject? targetObject = NWScript.GetSpellTargetObject().ToNwObject();
        if (targetObject == null)
        {
            return;
        }

        if (NWScript.GetIsFriend(targetObject) == NWScript.FALSE)
        {
            player.SendServerMessage(FriendsOnly);
            return;
        }

        if (NWScript.GetIsDead(targetObject) == NWScript.TRUE) return;

        NwCreature? creature = targetObject as NwCreature;
        if (creature == null)
        {
            Log.Warn("Defenders Duty called with no creature target.");
            return;
        }

        DefendersDuty duty = new(player, creature);

        duty.Apply();

        ActiveDuties.Add(creature, duty);
        if (creature.IsPlayerControlled(out NwPlayer? targetedPlayer))
        {
            player.OnClientLeave += StopTrackingDuty;
        }
    }

    private void StopTrackingDuty(ModuleEvents.OnClientLeave obj)
    {
        if (obj.Player.LoginCreature != null)
        {
            DefendersDuty duty = ActiveDuties[obj.Player.LoginCreature];

            duty.Stop();
        }

        if (obj.Player.LoginCreature != null && ActiveDuties.Remove(obj.Player.LoginCreature))
        {
            obj.Player.OnClientLeave -= StopTrackingDuty;
        }
    }
}