using AmiaReforged.Classes.Spells.Invocations.Lesser;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Services;

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
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_dreadseizure")]
    public void OnDreadSeizure(CallInfo info)
    {
        DreadSeizure script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_curse")]
    public void OnCurseOfDespair(CallInfo info)
    {
        CurseOfDespair script = new();
        script.Run(info.ObjectSelf);
    }
}