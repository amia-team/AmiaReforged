using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class WholenessOfBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.WholenessOfBody;

    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentWholenessOfBody(monk);
    }

    /// <summary>
    ///     Wholeness of Body deals 2d6 elemental damage in a large area round the monk, with a successful reflex save
    ///     halving the damage. Each Ki Focus adds 2d6 damage to a maximum of 8d6 elemental damage.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk)
    {
        if (monk.Location == null || !monk.IsInCombat) return;

        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);

        monk.ApplyEffect(EffectDuration.Instant, meteor.AoeVfx);
        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true,
                     ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Placeable))
        {
            int damageAmount = Random.Shared.Roll(6, meteor.DiceAmount);
            if (nwObject is not NwCreature creature)
            {
                _ = ApplyAoeDamage(nwObject, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
                continue;
            }
            if (monk.IsReactionTypeFriendly(creature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creature, NwSpell.FromSpellType(Spell.Fireball)!);

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
