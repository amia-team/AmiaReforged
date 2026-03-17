using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

[ServiceBinding(typeof(IInvocation))]
public class FleeTheScene : IInvocation
{
    public string ImpactScript => "wlk_fleethescene";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        Effect haste = Effect.LinkEffects(Effect.Haste(), Effect.VisualEffect(VfxType.DurCessatePositive));
        haste.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);
        warlock.ApplyEffect(EffectDuration.Temporary, haste, duration);

        if (!warlock.IsInCombat) return;

        int invocationDc = warlock.InvocationDc(invocationCl);
        warlock.ApplyEffect(EffectDuration.Temporary, Effect.Sanctuary(invocationDc), TimeSpan.FromSeconds(3f));
    }
}
