using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Cast;

[ServiceBinding(typeof(ITechnique))]
public class KiShout(AugmentationFactory augmentationFactory) : ICastTechnique
{
    public TechniqueType Technique => TechniqueType.KiShout;

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue
            ? augmentationFactory.GetAugmentation(path.Value, Technique)
            : null;

        if (augmentation is IAugmentation.ICastAugment castAugment)
        {
            castAugment.ApplyCastAugmentation(monk, castData, BaseTechnique);
        }
        else
        {
            BaseTechnique();
        }

        return;

        void BaseTechnique() => DoKiShout(monk);
    }

    /// <summary>
    ///     Stuns enemies within colossal range for three rounds if they fail a will save. In addition,
    ///     all enemies take 1d4 sonic damage per monk level. Each use depletes a Spirit Ki Point.
    /// </summary>
    public static void DoKiShout(NwCreature monk, DamageType damageType = DamageType.Sonic,
        VfxType damageVfx = VfxType.ImpSonic)
    {
        if (monk.Location == null) return;
        Effect kiShoutVfx = Effect.VisualEffect(VfxType.FnfHowlMind);

        monk.ApplyEffect(EffectDuration.Instant, kiShoutVfx);
        Effect stunEffect = Effect.Stunned();

        foreach (NwCreature hostileCreature in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Colossal, false))
        {
            if (!monk.IsReactionTypeHostile(hostileCreature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, hostileCreature, NwSpell.FromSpellType(Spell.AbilityHowlSonic)!);

            ApplyKiShoutEffects(monk, hostileCreature, damageType, damageVfx, stunEffect);
        }
    }

    private static void ApplyKiShoutEffects(NwCreature monk, NwCreature target, DamageType damageType,
        VfxType damageVfx, Effect stunEffect)
    {
        int dc = MonkUtils.CalculateMonkDc(monk);
        byte damageDice = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int damageAmount = Random.Shared.Roll(4, damageDice);

        float delay = monk.Distance(target) / 10;

        _ = ApplyDamage(target, monk, damageAmount, damageType, damageVfx, delay);

        SavingThrowResult savingThrowResult = target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                break;
            case SavingThrowResult.Success:
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
                break;
            case SavingThrowResult.Failure:
                _ = ApplyKiShoutStun(stunEffect, target, delay);
                break;
        }
    }

    private static async Task ApplyDamage(NwCreature target, NwCreature monk, int damageAmount,
        DamageType damageType, VfxType damageVfx, float delay)
    {

        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        await monk.WaitForObjectContext();
        Effect damageEffect = Effect.Damage(damageAmount, damageType);
        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(damageVfx));
    }

    private static async Task ApplyKiShoutStun(Effect stunEffect, NwCreature target, float delay)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        stunEffect.SubType = EffectSubType.Supernatural;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);
        target.ApplyEffect(EffectDuration.Temporary, stunEffect, effectDuration);
    }
}
