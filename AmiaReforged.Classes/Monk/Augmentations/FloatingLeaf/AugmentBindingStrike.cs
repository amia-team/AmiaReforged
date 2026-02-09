using AmiaReforged.Classes.Monk.Techniques.Attack;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentBindingStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.BindingStrike;

    /// <summary>
    /// Bypasses mind immunity, causing a weaker effect. Ki Focus I pacifies, II dazes, and III stuns.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        SavingThrowResult bindingStrikeResult = BindingStrike.DoBindingStrike(monk, attackData);

        if (attackData.Target is not NwCreature targetCreature
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
