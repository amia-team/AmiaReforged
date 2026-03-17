using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

[ServiceBinding(typeof(IInvocation))]
public class ShadowShape : IInvocation
{
    public string ImpactScript => "wlk_retinvis";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        Effect shadowShape = Effect.LinkEffects
            (
                Effect.VisualEffect(VfxType.DurGhostTransparent),
                Effect.VisualEffect(VfxType.DurGhostSmoke2),
                Effect.SavingThrowIncrease(SavingThrow.All, 4, SavingThrowType.Death),
                Effect.SavingThrowIncrease(SavingThrow.All, 4, SavingThrowType.Negative),
                Effect.Concealment(50)
            );
        shadowShape.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromTurns(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, shadowShape, duration);
    }
}
