using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Body;

[ServiceBinding(typeof(ITechnique))]
public class KiBarrier(AugmentationFactory augmentationFactory) : ITechnique
{
    private const string KiBarrierTag = nameof(TechniqueType.KiBarrier);
    public TechniqueType TechniqueType => TechniqueType.KiBarrier;

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyCastAugmentation(monk, TechniqueType, castData);
        else
            DoKiBarrier(monk);
    }

    /// <summary>
    /// The monk gains a +1 wisdom bonus. Each Ki Focus increases the bonus by +1, to a maximum of +4 at level 30 monk.
    /// </summary>
    public static void DoKiBarrier(NwCreature monk)
    {
        Effect? existingKiBarrier = monk.ActiveEffects.FirstOrDefault(e => e.Tag == KiBarrierTag);
        if (existingKiBarrier != null) monk.RemoveEffect(existingKiBarrier);

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte bonusWis = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect kiBarrier = Effect.LinkEffects(
            Effect.AbilityIncrease(Ability.Wisdom, bonusWis),
            Effect.VisualEffect(VfxType.DurCessatePositive)
        );
        kiBarrier.SubType = EffectSubType.Supernatural;
        kiBarrier.Tag = KiBarrierTag;

        Effect kiBarrierVfx = Effect.VisualEffect(VfxType.ImpDeathWard, false, 0.7f);

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrier, NwTimeSpan.FromTurns(monkLevel));
        monk.ApplyEffect(EffectDuration.Instant, kiBarrierVfx);
    }

    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData) { }
    public void HandleDamageTechnique(NwCreature monk, OnCreatureDamage damageData) { }
}
