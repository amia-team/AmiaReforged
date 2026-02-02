using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class QuiveringPalm : IAugmentation.ICastAugment
{
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.QuiveringPalm;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentQuiveringPalm(monk, castData);
    }

    /// <summary>
    /// Quivering Palm deals negative damage with a 25% multiplier, with extra 25% per Ki Focus.
    /// Overflow: Deals divine damage instead, and the monk is healed for 50% of their missing hit points.
    /// </summary>
    private void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData)
    {
        if (castData.TargetObject is not NwCreature targetCreature) return;

        bool hasOverflow = Overflow.HasOverflow(monk);

        DamageType damageType = hasOverflow switch
        {
            true => DamageType.Divine,
            false => DamageType.Negative
        };

        int pctVulnerability = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 50,
            KiFocus.KiFocus2 => 75,
            KiFocus.KiFocus3 => 100,
            _ => 25
        };

        Effect splinteredQuivering = Effect.DamageImmunityDecrease(damageType, pctVulnerability);
        splinteredQuivering.SubType = EffectSubType.Extraordinary;

        targetCreature.ApplyEffect(EffectDuration.Temporary, splinteredQuivering, TimeSpan.FromSeconds(0.5));

        TouchAttackResult touchAttackResult = Techniques.Cast.QuiveringPalm.DoQuiveringPalm(monk, castData, damageType);

        if (touchAttackResult == TouchAttackResult.Miss) return;

        VfxType vfxType = hasOverflow switch
        {
            true => VfxType.ImpSunstrike,
            false => VfxType.ImpNegativeEnergy
        };

        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfxType, fScale: 2f));

        if (!hasOverflow) return;

        int missingHp = monk.MaxHP - monk.HP;
        int healAmount = missingHp / 2;

        monk.ApplyEffect(EffectDuration.Instant, Effect.Heal(healAmount));
        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingG));
    }
}
