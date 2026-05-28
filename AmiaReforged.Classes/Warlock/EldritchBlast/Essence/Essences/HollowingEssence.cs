using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class HollowingEssence(ScriptHandleFactory scriptHandleFactory) : IEssence
{
    public EssenceType Essence => EssenceType.Hollowing;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Magical,
        SavingThrow: SavingThrow.Will,
        SavingThrowType: SavingThrowType.MindSpells,
        DmgImpVfx: VfxType.ImpMagblue,
        BeamVfx: VfxType.BeamMind,
        DoomVfx: AmiaVfxTypes.FnfDoomMind,
        PulseVfx: AmiaVfxTypes.ImpPulseMindChest,
        HideousBlowVfx: AmiaItemVisuals.Mind,
        Effect: HollowingEffect(invocationCl),
        AllowStacking: true,
        EffectImpVfx: VfxType.ImpDominateS
    );

    private Effect HollowingEffect(int invocationCl)
    {
        ScriptCallbackHandle onApply = scriptHandleFactory.CreateUniqueHandler(info =>
        {
            if (info.ObjectSelf is not NwCreature targetCreature)
                return ScriptHandleResult.Handled;

            TimeSpan duration = EssenceDuration(invocationCl);

            int wisdomDamage = Random.Shared.Roll(6);
            targetCreature.ApplyEffect(EffectDuration.Temporary, Effect.AbilityDecrease(Ability.Wisdom, wisdomDamage), duration);
            if (targetCreature.GetAbilityScore(Ability.Wisdom) - wisdomDamage <= 3)
                targetCreature.ApplyEffect(EffectDuration.Temporary, Effect.Stunned(), duration);

            return ScriptHandleResult.Handled;
        });
        return Effect.RunAction(onAppliedHandle: onApply);
    }

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
