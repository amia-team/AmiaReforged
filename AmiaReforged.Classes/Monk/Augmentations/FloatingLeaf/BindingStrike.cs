using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class BindingStrike : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.BindingStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        AugmentBindingStrike(monk, damageData);
    }

    /// <summary>
    /// Binding Strike does weaker effects if the target is immune to stun. Ki Focus I pacifies (making the
    /// target unable to attack), Ki Focus II dazes, and Ki Focus III paralyzes the target.
    /// </summary>
    private static void AugmentBindingStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        SavingThrowResult bindingStrikeResult = Techniques.Attack.BindingStrike.DoBindingStrike(monk, damageData);

        if (damageData.Target is not NwCreature targetCreature
            || bindingStrikeResult != SavingThrowResult.Immune)
            return;

        Effect? bindingEffect = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1  => Effect.Pacified(),
            KiFocus.KiFocus2 => Effect.Dazed(),
            KiFocus.KiFocus3 => Effect.Stunned(),
            _ => null
        };

        if (bindingEffect is null) return;

        Effect bindingVfx = Effect.VisualEffect(VfxType.FnfHowlOdd, false, 0.06f);
        bindingEffect.IgnoreImmunity = true;

        targetCreature.ApplyEffect(EffectDuration.Temporary, bindingEffect, NwTimeSpan.FromRounds(1));
        targetCreature.ApplyEffect(EffectDuration.Instant, bindingVfx);
    }
}
