using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentBindingStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.BindingStrike;

    /// <summary>
    /// Deals 2d6 elemental damage in a large radius (reflex halves). Each Ki Focus adds +2d6 damage.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData,
        BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        if (attackData.Target.Location is not { } location) return;

        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
        Effect damageVfx = Effect.VisualEffect(meteor.DamageVfx);

        attackData.Target.ApplyEffect(EffectDuration.Instant, meteor.PulseVfx);

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Medium, true))
        {
            if (!monk.IsReactionTypeHostile(creature)) continue;
            int damageAmount = Random.Shared.Roll(6, meteor.DiceAmount);

            bool hasEvasion = creature.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creature.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creature.RollSavingThrow(SavingThrow.Reflex, meteor.Dc, meteor.SaveType, monk);

            if ((hasEvasion || hasImprovedEvasion) && savingThrowResult == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, reflexVfx);
                continue;
            }

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            creature.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageAmount, meteor.DamageType));
            creature.ApplyEffect(EffectDuration.Instant, damageVfx);
        }
    }
}
