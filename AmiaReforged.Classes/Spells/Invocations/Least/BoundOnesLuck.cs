using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

[ServiceBinding(typeof(IInvocation))]
public class BoundOnesLuck : IInvocation
{
    public string ImpactScript => "wlk_boundluck";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (warlock.KnowsFeat(Feat.PrestigeDarkBlessing!))
        {
            warlock.ControllingPlayer?.FloatingTextString("You already have Dark Blessing.", false);
            return;
        }

        int savesCap = invocationCl / 7;
        if (invocationCl == 30)
            savesCap += 6;

        int uniSavesBonus = Math.Min(savesCap, warlock.GetAbilityModifier(Ability.Charisma));

        Effect boundOnesLuck = Effect.LinkEffects(Effect.SavingThrowIncrease(SavingThrow.All, uniSavesBonus),
            Effect.VisualEffect(VfxType.DurCessatePositive));
        boundOnesLuck.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromHours(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, boundOnesLuck, duration);
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadOdd));
    }
}
