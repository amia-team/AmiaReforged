using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Warlock;

public interface IInvocation
{
    string ImpactScript { get; }

    void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData);
}
