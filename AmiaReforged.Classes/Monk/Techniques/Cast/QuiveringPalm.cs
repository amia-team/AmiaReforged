using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Cast;

[ServiceBinding(typeof(ITechnique))]
public class QuiveringPalm(AugmentationFactory augmentationFactory) : ICastTechnique
{
    public TechniqueType Technique => TechniqueType.QuiveringPalm;

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue
            ? augmentationFactory.GetAugmentation(path.Value, Technique)
            : null;

        if (augmentation is IAugmentation.ICastAugment castAugment)
        {
            castAugment.ApplyCastAugmentation(monk, castData, BaseTechnique);
        }
        else
        {
            BaseTechnique();
        }

        return;

        void BaseTechnique() => DoQuiveringPalm(monk, castData);
    }

    /// <summary>
    /// On a successful melee touch attack against an enemy creature, the target must make a fortitude save or die.
    /// If the target survives, it takes 1d6 bludgeoning damage per monk level.
    /// </summary>
    public static TouchAttackResult DoQuiveringPalm(NwCreature monk, OnSpellCast castData,
        DamageType damageType = DamageType.Bludgeoning)
    {
        if (castData.TargetObject is not NwCreature targetCreature)
            return TouchAttackResult.Miss;

        CreatureEvents.OnSpellCastAt.Signal(monk, targetCreature, Spell.VampiricTouch!);

        TouchAttackResult touchAttackResult = monk.TouchAttackMelee(targetCreature);

        if (touchAttackResult is TouchAttackResult.Miss) return touchAttackResult;

        ApplyQuiveringDamage(monk, targetCreature, damageType);
        RollQuiveringDeath(monk, targetCreature);

        return touchAttackResult;
    }

    private static void ApplyQuiveringDamage(NwCreature monk, NwCreature targetCreature, DamageType damageType)
    {
        int damageDice = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int damageAmount = Random.Shared.Roll(6, damageDice);

        Effect quiveringDamage = Effect.LinkEffects(Effect.Damage(damageAmount, damageType),
            Effect.VisualEffect(VfxType.ImpDivineStrikeHoly, false, 0.2f));

        targetCreature.ApplyEffect(EffectDuration.Instant, quiveringDamage);
    }

    private static void RollQuiveringDeath(NwCreature monk, NwCreature targetCreature)
    {
        int dc = MonkUtils.CalculateMonkDc(monk);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Death, monk);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                break;
            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                break;
            case SavingThrowResult.Failure:
                ApplyQuiveringDeath(targetCreature);
                break;
        }
    }

    private static void ApplyQuiveringDeath(NwCreature targetCreature)
    {
        Effect quiveringEffect = Effect.Death(true);
        Effect quiveringVfx = Effect.VisualEffect(VfxType.ImpDeath, false, 0.7f);

        targetCreature.ApplyEffect(EffectDuration.Instant, quiveringEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, quiveringVfx);
    }
}
