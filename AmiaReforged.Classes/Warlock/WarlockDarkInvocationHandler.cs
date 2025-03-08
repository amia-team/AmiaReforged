using AmiaReforged.Classes.Spells.Invocations.Dark;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockDarkInvocationHandler))]
public class WarlockDarkInvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockDarkInvocationHandler()
    {
        Log.Info(message: "Warlock Dark Invocation Script Handler initialized.");
    }

    [ScriptHandler(scriptName: "wlk_darksight")]
    public void OnDarkForesight(CallInfo info)
    {
        DarkForesight script = new();
        script.CastDarkForesight(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_devourmagic")]
    public void OnDevourMagic(CallInfo info)
    {
        DevourMagic script = new();
        script.CastDevourMagic(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_retinvis")]
    public void OnShadowShape(CallInfo info)
    {
        ShadowShape script = new();
        script.CastShadowShape(info.ObjectSelf);
    }

    [ScriptHandler(scriptName: "wlk_wordchange")]
    public void OnWordOfChanging(CallInfo info)
    {
        WordOfChanging script = new();
        script.CastWordOfChanging(info.ObjectSelf);
    }
}