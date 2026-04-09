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
        Log.Info(message: "Warlock Pact Invocation Script Handler initialized.");
    }
}
