using Amia.Racial.Races.Script;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace Amia.Racial.Services;

[ServiceBinding(typeof(RaceEffectHandler))]
public class RaceEffectHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RaceEffectHandler()
    {
        Log.Info("Race Effect Handler initialized.");
    }


    [ScriptHandler("subrace_effects")]
    private void OnEffectsCalled(CallInfo info)
    {
        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;

        SubraceEffects.Apply(player.LoginCreature);
    }

    [ScriptHandler("race_effects")]
    private void OnBaseRaceEffectsCalled(CallInfo info)
    {
        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;

        RaceEffects.Apply(player.LoginCreature);
    }

    [ScriptHandler("heritage_setup")]
    private void OnHeritageSetupCalled(CallInfo info)
    {
        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;

        HeritageFeatSetup.Setup(player);
    }
}