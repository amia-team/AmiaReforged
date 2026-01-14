using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefenderScriptHandlers))]
public class DefenderScriptHandlers
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DefendersDutyFactory _abilityFactory;

    public DefenderScriptHandlers(DefendersDutyFactory abilityFactory)
    {
        _abilityFactory = abilityFactory;
        Log.Info("Setup Defender Script Handlers.");
    }

    /// <summary>
    ///     Script handler for defenders duty. Toggles the Defender's Duty aura which:
    ///     - Protects friendly creatures in the aura (absorbs 25% of their damage)
    ///     - Taunts hostile creatures in the aura (Will save vs Taunt skill)
    /// </summary>
    [ScriptHandler(scriptName: "def_duty")]
    public void OnDefendersDuty(CallInfo info)
    {
        if (info.ObjectSelf == null)
        {
            Log.Warn("Defenders Duty called with no object self.");
            return;
        }

        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer? player)) return;

        DefendersDuty duty = _abilityFactory.CreateDefendersDuty(player);
        duty.Apply();
    }
}
