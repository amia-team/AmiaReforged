using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class StunningStrike : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.StunningStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        AugmentStunningStrike(damageData);
    }

    /// <summary>
    /// Stunning Strike does weaker effects if the target is immune to stun. Ki Focus I pacifies (making the
    /// target unable to attack), Ki Focus II dazes, and Ki Focus III paralyzes the target.
    /// </summary>
    private static void AugmentStunningStrike(OnCreatureDamage damageData)
    {
        SavingThrowResult stunningStrikeResult = Techniques.Attack.StunningStrike.DoStunningStrike(damageData);

        if (damageData.Target is not NwCreature targetCreature
            || damageData.DamagedBy is not NwCreature monk
            || stunningStrikeResult != SavingThrowResult.Immune)
            return;

        Effect? stunningEffect = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1  => Effect.Pacified(),
            KiFocus.KiFocus2 => Effect.Dazed(),
            KiFocus.KiFocus3 => Effect.Stunned(),
            _ => null
        };

        if (stunningEffect is null) return;

        Effect stunningVfx = Effect.VisualEffect(VfxType.FnfHowlOdd, false, 0.06f);
        stunningEffect.IgnoreImmunity = true;

        targetCreature.ApplyEffect(EffectDuration.Temporary, stunningEffect, NwTimeSpan.FromRounds(1));
        targetCreature.ApplyEffect(EffectDuration.Instant, stunningVfx);
    }
}
