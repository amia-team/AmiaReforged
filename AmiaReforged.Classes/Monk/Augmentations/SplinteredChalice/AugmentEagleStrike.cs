using AmiaReforged.Classes.Monk.Techniques.Attack;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEagleStrike : IAugmentation.IDamageAugment
{
    private const string SplinteredEagleTag = nameof(PathType.SplinteredChalice) + nameof(TechniqueType.EagleStrike);
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.EagleStrike;

    /// <summary>
    /// Eagle Strike inflicts 3% physical and negative vulnerability, with extra 3% per Ki Focus.
    /// Overflow: Inflicts 5% physical and divine vulnerability, with extra 5% per Ki Focus.
    /// </summary>
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        SavingThrowResult savingThrowResult = EagleStrike.DoEagleStrike(monk, damageData);

        if (savingThrowResult != SavingThrowResult.Success
            || damageData.Target is not NwCreature targetCreature) return;

        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);
        bool hasOverflow = Overflow.HasOverflow(monk);

        int pctVulnerability = hasOverflow switch
        {
            true => kiFocus switch
            {
                KiFocus.KiFocus1 => 10,
                KiFocus.KiFocus2 => 15,
                KiFocus.KiFocus3 => 20,
                _ => 5
            },
            false => kiFocus switch
            {
                KiFocus.KiFocus1 => 6,
                KiFocus.KiFocus2 => 9,
                KiFocus.KiFocus3 => 12,
                _ => 3
            }
        };

        DamageType bonusDamageType = hasOverflow switch
        {
            true => DamageType.Divine,
            false => DamageType.Negative
        };

        VfxType vfxType = hasOverflow switch
        {
            true => VfxType.ImpSunstrike,
            false => VfxType.ImpNegativeEnergy
        };

        Effect? splinteredEagle = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == SplinteredEagleTag);
        if (splinteredEagle != null)
            targetCreature.RemoveEffect(splinteredEagle);

        splinteredEagle = Effect.LinkEffects
        (
            Effect.DamageImmunityDecrease(DamageType.Bludgeoning, pctVulnerability),
            Effect.DamageImmunityDecrease(DamageType.Slashing, pctVulnerability),
            Effect.DamageImmunityDecrease(DamageType.Piercing, pctVulnerability),
            Effect.DamageImmunityDecrease(bonusDamageType, pctVulnerability)
        );
        splinteredEagle.SubType = EffectSubType.Extraordinary;
        splinteredEagle.Tag = SplinteredEagleTag;

        targetCreature.ApplyEffect(EffectDuration.Temporary, splinteredEagle, NwTimeSpan.FromRounds(2));
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfxType));
    }
}
