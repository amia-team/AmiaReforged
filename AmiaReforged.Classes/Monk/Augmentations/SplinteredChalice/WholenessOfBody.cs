using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class WholenessOfBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.WholenessOfBody;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentWholenessOfBody(monk);
    }

    /// <summary>
    /// Wholeness of Body deals 10 negative damage in a large radius, with extra 10 damage per Ki Focus.
    /// Overflow: 20 divine damage in a large radius, with extra 20 damage per Ki Focus.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk)
    {
        if (monk.Location == null || !monk.IsInCombat) return;

        int diceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 20,
            KiFocus.KiFocus2 => 15,
            KiFocus.KiFocus3 => 10,
            _ => 5
        };

        int dc = MonkUtils.CalculateMonkDc(monk);

        Effect aoeVfx = MonkUtils.ResizedVfx(VfxType.FnfLosEvil30, RadiusSize.Large);

        monk.ApplyEffect(EffectDuration.Instant, aoeVfx);
        foreach (NwCreature hostileCreature in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, false))
        {
            if (!monk.IsReactionTypeHostile(hostileCreature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, hostileCreature, NwSpell.FromSpellType(Spell.NegativeEnergyBurst)!);

            int damageAmount = Random.Shared.Roll(10, diceAmount);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                damageAmount /= 2;
                hostileCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            }

            _ = ApplyWholenessDamage(hostileCreature, monk, damageAmount);
        }
    }

    private static async Task ApplyWholenessDamage(NwCreature hostileCreature, NwCreature monk, int damageAmount)
    {
        await monk.WaitForObjectContext();
        Effect wholenessEffect = Effect.LinkEffects(
            Effect.Damage(damageAmount, DamageType.Negative),
            Effect.Damage(damageAmount, DamageType.Piercing),
            Effect.VisualEffect(VfxType.ImpNegativeEnergy)
        );

        hostileCreature.ApplyEffect(EffectDuration.Instant, wholenessEffect);
    }
}
