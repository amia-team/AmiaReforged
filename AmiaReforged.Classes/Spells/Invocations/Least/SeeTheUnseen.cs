using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

[ServiceBinding(typeof(IInvocation))]
public class SeeTheUnseen : IInvocation
{
    public string ImpactScript => "wlk_see_unseen";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        Effect seeUnseen = Effect.LinkEffects
        (
            Effect.VisualEffect(VfxType.DurMagicalSight),
            Effect.SeeInvisible(),
            Effect.Ultravision()
        );
        seeUnseen.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromHours(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, seeUnseen, duration);
    }
}
