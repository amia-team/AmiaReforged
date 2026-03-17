using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

[ServiceBinding(typeof(IInvocation))]
public class LeapsAndBounds : IInvocation
{
    public string ImpactScript => "wlk_leapsbounds";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        Effect leaps = Effect.LinkEffects
        (
            Effect.AbilityIncrease(Ability.Dexterity, 4),
            Effect.SkillIncrease(Skill.Tumble!, 8),
            Effect.VisualEffect(VfxType.DurCessatePositive)
        );
        leaps.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromHours(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, leaps, duration);
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpImproveAbilityScore));
    }
}
