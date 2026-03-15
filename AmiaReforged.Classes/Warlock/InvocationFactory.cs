using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(InvocationFactory))]
public class InvocationFactory
{
    private readonly Dictionary<string, IInvocation> _invocations;

    public InvocationFactory(IEnumerable<IInvocation> invocations)
        => _invocations = invocations.ToDictionary(i => i.ImpactScript);

    public void CastInvocation(string scriptName, NwCreature warlock, int warlockLevel, int dc,
        SpellEvents.OnSpellCast castData)
    {
        if (!_invocations.TryGetValue(scriptName, out IInvocation? invocation)) return;

        invocation.CastInvocation(warlock, warlockLevel, castData);
    }
}
