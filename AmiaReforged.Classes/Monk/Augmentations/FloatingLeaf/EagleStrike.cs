using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class EagleStrike : IAugmentation.IDamageAugment
{
    private const string FloatingEagleStrikeTag = nameof(PathType.FloatingLeaf) +  nameof(TechniqueType.EagleStrike);
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.EagleStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        AugmentEagleStrike(monk, damageData);
    }

    /// <summary>
    /// Eagle Strike with Ki Focus I incurs a -1 penalty to attack rolls, increased to -2 with Ki Focus II and -3 with Ki Focus III.
    /// </summary>
    private static void AugmentEagleStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        SavingThrowResult bindingStrikeResult = Techniques.Attack.EagleStrike.DoEagleStrike(monk, damageData);

        if (damageData.Target is not NwCreature targetCreature || bindingStrikeResult != SavingThrowResult.Failure)
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
