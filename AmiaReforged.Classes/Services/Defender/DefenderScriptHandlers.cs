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
    private readonly DefendersDutyFactory _abilityFactory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private IDictionary<NwObject, DefendersDuty> ActiveDuties { get; set; }

    private const string FriendsOnly = "This ability can only be used on friendly creatures.";


    public DefenderScriptHandlers(DefendersDutyFactory abilityFactory)
    {
        _abilityFactory = abilityFactory;
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

        DefendersDuty duty = _abilityFactory.CreateDefendersDuty(player, creature);

        duty.Apply();
    }
}