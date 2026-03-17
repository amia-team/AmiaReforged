using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class DarkForesight : IInvocation
{
    public string ImpactScript => "wlk_darksight";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        int damageReduction = 10 + warlock.GetAbilityModifier(Ability.Charisma);
        int damageAbsorption = 10 * invocationCl;

        Effect darkForesight = Effect.LinkEffects(Effect.VisualEffect(VfxType.DurProtPremonition),
            Effect.DamageReduction(damageReduction, DamagePower.Plus5, damageAbsorption));
        darkForesight.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromTurns(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, darkForesight, duration);
    }
}
