using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefenderScriptHandlers))]
public class DefenderScriptHandlers
{
    private const string FriendsOnly = "This ability can only be used on friendly creatures.";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DefendersDutyFactory _abilityFactory;


    public DefenderScriptHandlers(DefendersDutyFactory abilityFactory)
    {
        _abilityFactory = abilityFactory;
        Log.Info(message: "Setup Defender Script Handlers.");
    }


    /// <summary>
    ///     Script handler for defenders duty. Provides a mechanism to override the damage done
    ///     to a defended target.
    /// </summary>
    /// <param name="info">
    ///     Default object housing information about the call to a given script. See <see cref="CallInfo" /> for
    ///     more information or peruse the Anvil API documents online for more information
    /// </param>
    [ScriptHandler(scriptName: "todo_replace_me")]
    public void OnDefendersDuty(CallInfo info)
    {
        // Because this ability directly intervenes with the standard game loop's typical processes,
        // extra precaution needs to be taken that all of the values required are valid.
        if (info.ObjectSelf == null)
        {
            Log.Warn(message: "Defenders Duty called with no object self.");
            return;
        }

        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer? player)) return;

        NwObject? targetObject = NWScript.GetSpellTargetObject().ToNwObject();
        if (targetObject == null) return;

        if (NWScript.GetIsFriend(targetObject) == NWScript.FALSE)
        {
            player.SendServerMessage(FriendsOnly);
            return;
        }

        if (NWScript.GetIsDead(targetObject) == NWScript.TRUE) return;

        NwCreature? creature = targetObject as NwCreature;
        if (creature == null)
        {
            Log.Warn(message: "Defenders Duty called with no creature target.");
            return;
        }

        DefendersDuty duty = _abilityFactory.CreateDefendersDuty(player, creature);

        duty.Apply();
    }
}