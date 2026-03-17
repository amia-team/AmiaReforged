using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

[ServiceBinding(typeof(IInvocation))]
public class WalkUnseen : IInvocation
{
    public string ImpactScript => "wlk_walkunseen";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);
        warlock.ApplyEffect(EffectDuration.Temporary, Effect.Invisibility(InvisibilityType.Normal), duration);
    }
}
