using AmiaReforged.Classes.Spells.Invocations.Pact;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockPactInvocationHandler))]
public class WarlockPactInvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockPactInvocationHandler()
    {
        Log.Info("Warlock Pact Invocation Script Handler initialized.");
    }

    [ScriptHandler("wlk_louddecay")]
    public void OnLoudDecay(CallInfo info)
    {
        LoudDecay script = new();
        script.CastLoudDecay(info.ObjectSelf);
    }

    [ScriptHandler("wlk_lightscall")]
    public void OnLightsCalling(CallInfo info)
    {
        LightsCalling script = new();
        script.CastLightsCalling(info.ObjectSelf);
    }

    [ScriptHandler("wlk_dancingplag")]
    public void OnDancingPlague(CallInfo info)
    {
        DancingPlague script = new();
        script.CastDancingPlague(info.ObjectSelf);
    }

    [ScriptHandler("wlk_bindingmag")]
    public void OnBindingOfMaggots(CallInfo info)
    {
        BindingOfMaggots script = new();
        script.CastBindingOfMaggots(info.ObjectSelf);
    }

    [ScriptHandler("wlk_bindingent")]
    public void OnBindingOfMaggotsEnter(CallInfo info)
    {
        BindingOfMaggotsEnter script = new();
        script.BindingOfMaggotsEnterEffects(info.ObjectSelf);
    }

    [ScriptHandler("wlk_primordial")]
    public void OnPrimordialGust(CallInfo info)
    {
        PrimordialGust script = new();
        script.CastPrimordialGust(info.ObjectSelf);
    }

    [ScriptHandler("wlk_frogdrop")]
    public void OnFrogDrop(CallInfo info)
    {
        FrogDrop script = new();
        script.CastFrogDrop(info.ObjectSelf);
    }
}