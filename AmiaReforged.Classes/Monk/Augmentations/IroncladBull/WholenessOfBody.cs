using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class WholenessOfBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.WholenessOfBody;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentWholenessOfBody(monk);
    }

    /// <summary>
    /// Wholeness of Body heals for 20 extra hit points and grants overheal as temporary hit points.
    /// Each Ki Focus increases the amount of extra hit points healed by 20, to a maximum of 80 extra hit points.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk)
    {
        int healAmount = CalculateHealAmount(monk);
        int overhealAmount = Math.Max(0, healAmount - (monk.MaxHP - monk.HP));

        Effect wholenessEffect = Effect.Heal(healAmount);
        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);

        monk.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        monk.ApplyEffect(EffectDuration.Instant, wholenessVfx);

        ApplyOverheal(monk, overhealAmount);
    }

    private static void ApplyOverheal(NwCreature monk, int overhealAmount)
    {
        if (overhealAmount <= 0) return;

        Effect overhealEffect = Effect.LinkEffects(
            Effect.TemporaryHitpoints(overhealAmount),
            Effect.VisualEffect(VfxType.DurProtGreaterStoneskin)
        );

        overhealEffect.SubType = EffectSubType.Extraordinary;

        monk.ApplyEffect(EffectDuration.Permanent, overhealEffect);
    }

    private static int CalculateHealAmount(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte extraHeal = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 40,
            KiFocus.KiFocus2 => 60,
            KiFocus.KiFocus3 => 80,
            _ => 20
        };

        return monkLevel * 2 + extraHeal;
    }

}
