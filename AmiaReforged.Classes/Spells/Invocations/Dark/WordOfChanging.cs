using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

[ServiceBinding(typeof(IInvocation))]
public class WordOfChanging : IInvocation
{
    public string ImpactScript => "wlk_wordchange";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        // Reset invocation CL to warlock level, as this invocation decreases the invocation CL while active
        invocationCl = warlock.WarlockLevel();

        int abIncrease = Math.Min(5, invocationCl / 4);
        Effect wordOfChanging = Effect.LinkEffects
        (
            Effect.AttackIncrease(abIncrease),
            Effect.AbilityIncrease(Ability.Strength, Random.Shared.Roll(4)),
            Effect.AbilityIncrease(Ability.Dexterity, Random.Shared.Roll(4)),
            Effect.AbilityIncrease(Ability.Constitution, Random.Shared.Roll(4))
        );
        wordOfChanging.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, wordOfChanging, duration);
    }
}
