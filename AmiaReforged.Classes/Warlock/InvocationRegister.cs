using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(InvocationRegister))]
public class InvocationRegister
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly InvocationFactory _invocationFactory;

    public InvocationRegister(
        IEnumerable<IInvocation> invocations,
        ScriptHandleFactory scriptHandleFactory,
        InvocationFactory invocationFactory)
    {
        _invocationFactory = invocationFactory;


        foreach (IInvocation invocation in invocations)
        {
            if (string.IsNullOrEmpty(invocation.ImpactScript)) continue;

            scriptHandleFactory.RegisterScriptHandler(invocation.ImpactScript, HandleInvocation);
            Log.Info($"Hooked Warlock script: {invocation.ImpactScript}");
        }
    }

    private ScriptHandleResult HandleInvocation(CallInfo info)
    {
        SpellEvents.OnSpellCast eventData = new();
        if (eventData.Caster is not NwCreature warlock) return ScriptHandleResult.Handled;

        _invocationFactory.CastInvocation(
            info.ScriptName,
            warlock,
            warlock.GetInvocationCasterLevel(),
            eventData);

        return ScriptHandleResult.Handled;
    }
}
