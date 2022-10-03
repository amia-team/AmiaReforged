using AmiaReforged.Classes.Spells.Invocations.Greater;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Services;

[ServiceBinding(typeof(WarlockGreaterInvocationHandler))]
public class WarlockGreaterInvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockGreaterInvocationHandler()
    {
        Log.Info("Warlock Greater Invocation Script Handler initialized.");
    }

    [ScriptHandler("wlk_causticmire")]
    public void OnCausticMire(CallInfo info)
    {
        CausticMire script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_mireent")]
    public void OnCausticMireEnter(CallInfo info)
    {
        CausticMireOnEnter script = new();
        script.ApplyOnEnterEffects(info.ObjectSelf);
    }
    
    [ScriptHandler("wlk_mirehbea")]
    public void OnCausticMireHeartbeat(CallInfo info)
    {
        CausticMireHeartBeat script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_mireexit")]
    public void OnCausticMireExit(CallInfo info)
    {
        CausticMireOnExit script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_chilltentac")]
    public void OnChillingTentacles(CallInfo info)
    {
        ChillingTentacles script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_tentent")]
    public void OnChillingTentaclesEnter(CallInfo info)
    {
        ChillingTentaclesEnter script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_tenthbea")]
    public void OnChillingTentaclesHeartbeat(CallInfo info)
    {
        ChillingTentaclesHeartbeat script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_tenplague")]
    public void OnTenaciousPlague(CallInfo info)
    {
        TenaciousPlague script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_swarment")]
    public void OnTenaciousPlagueEnter(CallInfo info)
    {
        TenaciousPlagueEnter script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_swarmexit")]
    public void OnTenaciousPlagueExit(CallInfo info)
    {
        TenaciousPlagueExit script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_swarmhbea")]
    public void OnTenaciousPlagueHeartbeat(CallInfo info)
    {
        TenaciousPlagueHeartbeat script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_flamewall")]
    public void OnWallOfFlame(CallInfo info)
    {
        WallOfPerilousFlame script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_flamewalhbea")]
    public void OnWallOfFlameHeartbeat(CallInfo info)
    {
        WallOfPerilousFlameHeartbeat script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_flamewallent")]
    public void OnWallOfFlameEnter(CallInfo info)
    {
        WallOfPerilousFlameOnEnter script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_insid_shadws")]
    public void OnWrithingDark(CallInfo info)
    {
        WrithingDark script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_darkent")]
    public void OnWrithingDarkEnter(CallInfo info)
    {
        WrithingDarkEnter script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_darkhbea")]
    public void OnWrithingDarkHeartbeat(CallInfo info)
    {
        WrithingDarkHeartbeat script = new();
        script.Heartbeat(info.ObjectSelf);
    }

    [ScriptHandler("wlk_incanent")]
    public void OnIncandescentEnter(CallInfo info)
    {
        IncandescentOnEnter script = new();
        script.ApplyOnEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler("wlk_incanexit")]
    public void OnIncandescentExit(CallInfo info)
    {
        IncandescentOnExit script = new();
        script.RemoveIncandescentEffects();
    }

    [ScriptHandler("wlk_incanhbea")]
    public void OnIncandescentHeartbeat(CallInfo info)
    {
        IncandescentHeartbeat script = new();
        script.Heartbeat(info.ObjectSelf);
    }
}