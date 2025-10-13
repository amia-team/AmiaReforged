using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Body;

[ServiceBinding(typeof(ITechnique))]
public class WholenessOfBody(AugmentationFactory augmentationFactory) : ITechnique
{
    public TechniqueType TechniqueType => TechniqueType.WholenessOfBody;

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyCastAugmentation(monk, TechniqueType, castData);
        else
            DoWholenessOfBody(monk);
    }

    /// <summary>
    ///     The monk can heal damage equal to twice their class level. Each use depletes a Body Ki Point.
    /// </summary>
    public static void DoWholenessOfBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int healAmount = monkLevel * 2;
        Effect wholenessEffect = Effect.Heal(healAmount);

        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);

        monk.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        monk.ApplyEffect(EffectDuration.Instant, wholenessVfx);
    }

    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData) { }
}
