using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class StunningStrike : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.StunningStrike;

    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData,
        BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentStunningStrike(monk, damageData);
    }

    /// <summary>
    ///     Stunning Strike deals 2d6 elemental damage in a large area around the target. The damage isn’t multiplied by
    ///     critical hits and a successful reflex save halves the damage. Each Ki Focus adds 2d6 to a maximum of 8d6 elemental
    ///     damage.
    /// </summary>
    private void AugmentStunningStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        if (damageData.Target.Location is not { } location) return;

        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);

        damageData.Target.ApplyEffect(EffectDuration.Instant, meteor.PulseVfx);

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

            _ = ApplyAoeDamage(creature, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
        }
    }



    private static async Task ApplyAoeDamage(NwGameObject targetObject, NwCreature monk, int damageAmount,
        DamageType damageType, VfxType damageVfx)
    {
        await monk.WaitForObjectContext();
        Effect damageEffect = Effect.LinkEffects(
            Effect.Damage(damageAmount, damageType),
            Effect.VisualEffect(damageVfx));

        targetObject.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}
