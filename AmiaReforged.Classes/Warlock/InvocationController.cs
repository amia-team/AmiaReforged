using AmiaReforged.Classes.Warlock.EldritchBlast;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(InvocationController))]
public class InvocationController
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly InvocationFactory _invocationFactory;
    private readonly EldritchBlastHandler _eldritchBlastHandler;

    public InvocationController(
        EldritchBlastHandler eldritchBlastHandler,
        InvocationFactory invocationFactory,
        ScriptHandleFactory scriptHandleFactory,
        IEnumerable<IInvocation> invocations)
    {
        _invocationFactory = invocationFactory;
        _eldritchBlastHandler = eldritchBlastHandler;

        scriptHandleFactory.RegisterScriptHandler(WarlockExtensions.EldritchBlastImpactScript, HandleInvocation);
        Log.Info($"Hooked Warlock script (Eldritch Blast): {WarlockExtensions.EldritchBlastImpactScript}");

        foreach (IInvocation invocation in invocations)
        {
            if (string.IsNullOrEmpty(invocation.ImpactScript)) continue;

            scriptHandleFactory.RegisterScriptHandler(invocation.ImpactScript, HandleInvocation);
            Log.Info($"Hooked Warlock script: {nameof(invocation)}");
        }
    }

    private ScriptHandleResult HandleInvocation(CallInfo info)
    {
        SpellEvents.OnSpellCast eventData = new();
        if (eventData.Caster is not NwCreature warlock) return ScriptHandleResult.Handled;

        int invocationCl = warlock.GetInvocationCasterLevel();

        if (eventData.Spell.ImpactScript == WarlockExtensions.EldritchBlastImpactScript)
        {
            _eldritchBlastHandler.HandleEldritchBlast(warlock, invocationCl, eventData);
            return ScriptHandleResult.Handled;
        }

        _invocationFactory.CastInvocation(
            info.ScriptName,
            warlock,
            invocationCl,
            eventData);

        return ScriptHandleResult.Handled;
    }
}
