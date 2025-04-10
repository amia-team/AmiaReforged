﻿using AmiaReforged.Classes.Spells.Invocations.Lesser;
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
        Log.Info(message: "Warlock Lesser Invocation Script Handler initialized.");
    }

    [ScriptHandler(scriptName: "wlk_walkunseen")]
    public void OnWalkUnseen(CallInfo info)
    {
        WalkUnseen script = new();
        script.CastWalkUnseen(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_dreadseizure")]
    public void OnDreadSeizure(CallInfo info)
    {
        DreadSeizure script = new();
        script.CastDreadSeizure(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_dreadenter")]
    public void OnDreadSeizureEnter(CallInfo info)
    {
        DreadSeizureEnter script = new();
        script.DreadSeizureEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_dreadexit")]
    public void OnDreadSeizureExit(CallInfo info)
    {
        DreadSeizureExit script = new();
        script.DreadSeizureExitEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_curse")]
    public void OnCurseOfDespair(CallInfo info)
    {
        CurseOfDespair script = new();
        script.CastCurseOfDespair(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_fleethescene")]
    public void OnFleeTheScene(CallInfo info)
    {
        FleeTheScene script = new();
        script.CastFleeTheScene(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_insid_shadws")]
    public void OnWrithingDark(CallInfo info)
    {
        WrithingDark script = new();
        script.CastWrithingDark(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_darkent")]
    public void OnWrithingDarkEnter(CallInfo info)
    {
        WrithingDarkEnter script = new();
        script.WrithingDarkEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_darkhbea")]
    public void OnWrithingDarkHeartbeat(CallInfo info)
    {
        WrithingDarkHeartbeat script = new();
        script.WrithingDarkHeartbeatEffects(info.ObjectSelf);
    }
}