using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

[ServiceBinding(typeof(IInvocation))]
public class OtherworldlyWhispers : IInvocation
{
    public string ImpactScript => "wlk_othrwrldwhis";
    public void CastInvocation(NwCreature warlock, int warlockLevel, SpellEvents.OnSpellCast castData)
    {
        int skillBonus = 10 + warlockLevel / 2 + warlock.GetAbilityModifier(Ability.Charisma);
        Effect whispers = Effect.LinkEffects
        (
            Effect.SkillIncrease(Skill.Lore!, skillBonus),
            Effect.SkillIncrease(Skill.Spellcraft!, skillBonus),
            Effect.VisualEffect(VfxType.DurCessatePositive)
        );
        whispers.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromHours(warlockLevel);

        warlock.ApplyEffect(EffectDuration.Temporary, whispers, duration);
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpNightmareHeadHit));
    }
}
