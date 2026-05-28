using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class EntropicEssence(ScriptHandleFactory scriptHandleFactory) : IEssence
{
    public EssenceType Essence => EssenceType.Entropic;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Magical,
        SavingThrow: GetRandomSavingThrow,
        SavingThrowType: SavingThrowType.Chaos,
        DmgImpVfx: VfxType.ImpMagblue,
        BeamVfx: VfxType.BeamOdd,
        DoomVfx: AmiaVfxTypes.FnfDoomOdd,
        PulseVfx: AmiaVfxTypes.ImpPulseOddChest,
        HideousBlowVfx: AmiaItemVisuals.OddHue,
        EffectImpVfx: AmiaVfxTypes.FnfSunder,
        Effect: EntropicEffect(invocationCl),
        AllowStacking: true
    );

    private static SavingThrow GetRandomSavingThrow => Random.Shared.Roll(3) switch
    {
        1 => SavingThrow.Fortitude,
        2 => SavingThrow.Reflex,
        _ => SavingThrow.Will
    };

    private static Ability GetRandomAbility => Random.Shared.Roll(3) switch
    {
        1 => Ability.Wisdom,
        2 => Ability.Dexterity,
        _ => Ability.Constitution
    };

    private Effect EntropicEffect(int invocationCl)
    {
        TimeSpan duration = EssenceDuration(invocationCl);

        ScriptCallbackHandle onApply = scriptHandleFactory.CreateUniqueHandler(info =>
        {
            if (info.ObjectSelf is not NwCreature targetCreature)
                return ScriptHandleResult.Handled;

            targetCreature.ApplyEffect(EffectDuration.Temporary, AbilityDecreaseEffect, duration);
            return ScriptHandleResult.Handled;
        });

        return Effect.RunAction(onAppliedHandle: onApply);
    }

    private static Effect AbilityDecreaseEffect => Effect.LinkEffects(
        Effect.AbilityDecrease(GetRandomAbility, Random.Shared.Roll(6)),
        Effect.VisualEffect(VfxType.DurCessateNegative));

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
