using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Spirit;

[ServiceBinding(typeof(ITechnique))]
public class KiShout(AugmentationFactory augmentationFactory) : ITechnique
{
    public TechniqueType TechniqueType => TechniqueType.KiShout;
    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyCastAugmentation(monk, TechniqueType, castData);
        else
            DoKiShout(monk);
    }

    /// <summary>
    ///     Stuns enemies within colossal range for three rounds if they fail a will save. In addition,
    ///     all enemies take 1d4 sonic damage per monk level. Each use depletes a Spirit Ki Point.
    /// </summary>
    public static void DoKiShout(NwCreature monk, DamageType damageType = DamageType.Sonic, VfxType damageVfx = VfxType.ImpSonic)
    {
        if (monk.Location == null) return;
        Effect kiShoutVfx = Effect.VisualEffect(VfxType.FnfHowlMind);
        monk.ApplyEffect(EffectDuration.Instant, kiShoutVfx);

        foreach (NwGameObject obj in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            if (obj is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, hostileCreature, NwSpell.FromSpellType(Spell.AbilityHowlSonic)!);

            ApplyKiShoutEffects(monk, hostileCreature, damageType, damageVfx);
        }
    }

    private static void ApplyKiShoutEffects(NwCreature monk, NwCreature target, DamageType damageType, VfxType damageVfx)
    {
        int dc = MonkUtils.CalculateMonkDc(monk);
        byte damageDice = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int damageAmount = Random.Shared.Roll(4, damageDice);
        Effect damageEffect = Effect.Damage(damageAmount, damageType);

        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(damageVfx));

        SavingThrowResult savingThrowResult = target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                break;
            case SavingThrowResult.Success:
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
                break;
            case SavingThrowResult.Failure:
                ApplyKiShoutStun(target);
                break;
        }
    }

    private static void ApplyKiShoutStun(NwCreature target)
    {
        Effect kiShoutEffect = Effect.Stunned();
        kiShoutEffect.SubType = EffectSubType.Supernatural;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);
        target.ApplyEffect(EffectDuration.Temporary, kiShoutEffect, effectDuration);
    }

    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData) { }
}
