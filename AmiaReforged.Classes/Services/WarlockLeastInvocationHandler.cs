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
        if(!info.ObjectSelf.IsPlayerControlled(out NwPlayer player)) return;
        
        if (player.LoginCreature.Classes.Any(c => c.Class.Name.ToString() == "Blackguard"))
        {
            player.SendServerMessage("You can't stack Bound One's Own luck with Dark Blessing.");
            return;
        }
        
        BoundOnesLuck script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_leapsbounds")]
    public void OnLeapsAndBounds(CallInfo info)
    {
        LeapsAndBounds script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_othrwrldwhis")]
    public void OnOtherworldyWhispers(CallInfo info)
    {
        OtherworldlyWhispers script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_repelhail")]
    public void OnRepelHail(CallInfo info)
    {
        RepelTheHail script = new();
        script.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_see_unseen")]
    public void OnSeeUnseen(CallInfo info)
    {
        SeeTheUnseen script = new();
        script.Run(info.ObjectSelf);
    }
}