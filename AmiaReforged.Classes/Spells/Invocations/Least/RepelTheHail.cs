using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class RepelTheHail : IInvocation
{
    private const VfxType DurDeathWardMid = (VfxType)2543;
    public string ImpactScript => "wlk_repelhail";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        int rangedConcealment = 25 + invocationCl + warlock.GetAbilityModifier(Ability.Charisma) / 2;

        Effect repelHail = Effect.LinkEffects
        (
            Effect.Concealment(rangedConcealment, MissChanceType.VsRanged),
            Effect.VisualEffect(DurDeathWardMid),
            Effect.SkillIncrease(Skill.Hide!, 4),
            Effect.SkillIncrease(Skill.MoveSilently!, 4)
        );
        repelHail.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromTurns(invocationCl);

        warlock.ApplyEffect(EffectDuration.Temporary, repelHail, duration);
    }
}
