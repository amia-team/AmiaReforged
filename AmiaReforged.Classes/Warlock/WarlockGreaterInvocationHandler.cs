﻿using AmiaReforged.Classes.Spells.Invocations.Greater;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockGreaterInvocationHandler))]
public class WarlockGreaterInvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockGreaterInvocationHandler()
    {
        Log.Info(message: "Warlock Greater Invocation Script Handler initialized.");
    }

    [ScriptHandler(scriptName: "wlk_causticmire")]
    public void OnCausticMire(CallInfo info)
    {
        CausticMire script = new();
        script.CastCausticMire(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_mireent")]
    public void OnCausticMireEnter(CallInfo info)
    {
        CausticMireOnEnter script = new();
        script.CausticMireEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_mirehbea")]
    public void OnCausticMireHeartbeat(CallInfo info)
    {
        CausticMireHeartbeat script = new();
        script.CausticMireHeartbeatEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_mireexit")]
    public void OnCausticMireExit(CallInfo info)
    {
        CausticMireOnExit script = new();
        script.CausticMireExitEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_chilltentac")]
    public void OnChillingTentacles(CallInfo info)
    {
        ChillingTentacles script = new();
        script.CastChillingTentacles(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_tentent")]
    public void OnChillingTentaclesEnter(CallInfo info)
    {
        ChillingTentaclesEnter script = new();
        script.ChillingTentaclesEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_tenthbea")]
    public void OnChillingTentaclesHeartbeat(CallInfo info)
    {
        ChillingTentaclesHeartbeat script = new();
        script.ChillingTentaclesHeartbeatEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_tenplague")]
    public void OnTenaciousPlague(CallInfo info)
    {
        TenaciousPlague script = new();
        script.CastTenaciousPlague(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_swarment")]
    public void OnTenaciousPlagueEnter(CallInfo info)
    {
        TenaciousPlagueEnter script = new();
        script.TenaciousPlagueEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_swarmexit")]
    public void OnTenaciousPlagueExit(CallInfo info)
    {
        TenaciousPlagueExit script = new();
        script.TenaciousPlagueExitEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_swarmhbea")]
    public void OnTenaciousPlagueHeartbeat(CallInfo info)
    {
        TenaciousPlagueHeartbeat script = new();
        script.TenaciousPlagueHeartbeatEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_flamewall")]
    public void OnWallOfFlame(CallInfo info)
    {
        WallOfPerilousFlame script = new();
        script.CastWallOfFlame(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_flamewalhbea")]
    public void OnWallOfFlameHeartbeat(CallInfo info)
    {
        WallOfPerilousFlameHeartbeat script = new();
        script.WallOfFlameHeartbeatEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_flamewallent")]
    public void OnWallOfFlameEnter(CallInfo info)
    {
        WallOfPerilousFlameOnEnter script = new();
        script.WallOfFlameEnterEffects(info.ObjectSelf);
    }
}