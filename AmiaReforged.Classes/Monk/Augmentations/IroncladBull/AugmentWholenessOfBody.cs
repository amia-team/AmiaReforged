using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentWholenessOfBody : IAugmentation.ICastAugment
{
    private const string OverHealTag = nameof(PathType.IroncladBull) + nameof(TechniqueType.WholenessOfBody);
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.WholenessOfBody;

    /// <summary>
    /// Heals an additional 20 HP and converts excess healing into temporary HP. Each Ki Focus adds +20 healing.
    /// </summary>
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
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

        Effect? overhealEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == OverHealTag);
        if (overhealEffect != null) monk.RemoveEffect(overhealEffect);

        overhealEffect = Effect.LinkEffects(
            Effect.TemporaryHitpoints(overhealAmount),
            Effect.VisualEffect(VfxType.DurProtGreaterStoneskin)
        );

        overhealEffect.SubType = EffectSubType.Extraordinary;
        overhealEffect.Tag = OverHealTag;

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
