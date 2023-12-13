using AmiaReforged.Classes.Spells.Invocations.Least;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Services;

[ServiceBinding(typeof(WarlockLeastInvocationHandler))]
public class WarlockLeastInvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockLeastInvocationHandler()
    {
        Log.Info("Warlock Least Invocation Script Handler initialized.");
    }

    [ScriptHandler("wlk_boundluck")]
    public void OnBoundLuck(CallInfo info)
    {
        BoundOnesLuck script = new();
        script.CastBoundOnesLuck(info.ObjectSelf);
    }

    [ScriptHandler("wlk_leapsbounds")]
    public void OnLeapsAndBounds(CallInfo info)
    {
        LeapsAndBounds script = new();
        script.CastLeapsAndBounds(info.ObjectSelf);
    }

    [ScriptHandler("wlk_othrwrldwhis")]
    public void OnOtherworldyWhispers(CallInfo info)
    {
        OtherworldlyWhispers script = new();
        script.CastOtherworldlyWhispers(info.ObjectSelf);
    }

    [ScriptHandler("wlk_repelhail")]
    public void OnRepelTheHail(CallInfo info)
    {
        RepelTheHail script = new();
        script.CastRepelTheHail(info.ObjectSelf);
    }

    [ScriptHandler("wlk_see_unseen")]
    public void OnSeeTheUnseen(CallInfo info)
    {
        SeeTheUnseen script = new();
        script.CastSeeTheUnseen(info.ObjectSelf);
    }
}