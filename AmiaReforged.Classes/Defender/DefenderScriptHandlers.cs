using Anvil.API;
using Anvil.API.Events;
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
    [ScriptHandler(scriptName: "def_duty")]
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

        SpellEvents.OnSpellCast eventData = new();
        if (eventData.TargetObject is not NwCreature targetCreature) return;
        if (eventData.Caster is not NwCreature casterCreature) return;


        if (targetCreature.IsDead) return;


        if(targetCreature.IsEnemy(casterCreature))
        {
            player.SendServerMessage(FriendsOnly, ColorConstants.Red);
            return;
        }

        DefendersDuty duty = _abilityFactory.CreateDefendersDuty(player, targetCreature);

        duty.Apply();
    }
}
