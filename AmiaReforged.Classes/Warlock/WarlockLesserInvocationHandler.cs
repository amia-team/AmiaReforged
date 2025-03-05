using AmiaReforged.Classes.Spells.Invocations.Lesser;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockLesserInvocationHandler))]
public class WarlockLesserInvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockLesserInvocationHandler()
    {
        Log.Info("Warlock Lesser Invocation Script Handler initialized.");
    }

    [ScriptHandler("wlk_walkunseen")]
    public void OnWalkUnseen(CallInfo info)
    {
        WalkUnseen script = new();
        script.CastWalkUnseen(info.ObjectSelf);
    }

    [ScriptHandler("wlk_dreadseizure")]
    public void OnDreadSeizure(CallInfo info)
    {
        DreadSeizure script = new();
        script.CastDreadSeizure(info.ObjectSelf);
    }

    [ScriptHandler("wlk_dreadenter")]
    public void OnDreadSeizureEnter(CallInfo info)
    {
        DreadSeizureEnter script = new();
        script.DreadSeizureEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler("wlk_dreadexit")]
    public void OnDreadSeizureExit(CallInfo info)
    {
        DreadSeizureExit script = new();
        script.DreadSeizureExitEffects(info.ObjectSelf);
    }

    [ScriptHandler("wlk_curse")]
    public void OnCurseOfDespair(CallInfo info)
    {
        CurseOfDespair script = new();
        script.CastCurseOfDespair(info.ObjectSelf);
    }

    [ScriptHandler("wlk_fleethescene")]
    public void OnFleeTheScene(CallInfo info)
    {
        FleeTheScene script = new();
        script.CastFleeTheScene(info.ObjectSelf);
    }

        [ScriptHandler("wlk_insid_shadws")]
    public void OnWrithingDark(CallInfo info)
    {
        WrithingDark script = new();
        script.CastWrithingDark(info.ObjectSelf);
    }

    [ScriptHandler("wlk_darkent")]
    public void OnWrithingDarkEnter(CallInfo info)
    {
        WrithingDarkEnter script = new();
        script.WrithingDarkEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler("wlk_darkhbea")]
    public void OnWrithingDarkHeartbeat(CallInfo info)
    {
        WrithingDarkHeartbeat script = new();
        script.WrithingDarkHeartbeatEffects(info.ObjectSelf);
    }
}