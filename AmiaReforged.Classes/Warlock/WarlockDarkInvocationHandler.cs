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

    [ScriptHandler(scriptName: "wlk_wordchange")]
    public void OnWordOfChanging(CallInfo info)
    {
        WordOfChanging script = new();
        script.CastWordOfChanging(info.ObjectSelf);
    }
}
