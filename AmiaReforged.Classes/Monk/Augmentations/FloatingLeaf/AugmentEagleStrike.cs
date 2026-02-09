using AmiaReforged.Classes.Monk.Techniques.Attack;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEagleStrike : IAugmentation.IAttackAugment
{
    private const string FloatingEagleStrikeTag = nameof(PathType.FloatingLeaf) +  nameof(TechniqueType.EagleStrike);
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.EagleStrike;

    /// <summary>
    /// Inflicts -1 attack bonus penalty. Each Ki Focus adds -1.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        SavingThrowResult bindingStrikeResult = EagleStrike.DoEagleStrike(monk, attackData);

        if (attackData.Target is not NwCreature targetCreature || bindingStrikeResult != SavingThrowResult.Failure)
            return;

        int abDecrease = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 1,
            KiFocus.KiFocus2 => 2,
            KiFocus.KiFocus3 => 3,
            _ => 0
        };

        if (abDecrease == 0) return;

        Effect? eagleEffect = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == FloatingEagleStrikeTag);
        if (eagleEffect != null)
            targetCreature.RemoveEffect(eagleEffect);

        eagleEffect = Effect.AttackDecrease(abDecrease);
        eagleEffect.Tag = FloatingEagleStrikeTag;

        targetCreature.ApplyEffect(EffectDuration.Temporary, eagleEffect, NwTimeSpan.FromRounds(2));
    }
}
