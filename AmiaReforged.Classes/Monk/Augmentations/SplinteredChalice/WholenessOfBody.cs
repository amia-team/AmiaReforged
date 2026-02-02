using AmiaReforged.Classes.Monk.Constants;
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

        bool hasOverflow = Overflow.HasOverflow(monk);
        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);
        int dc = MonkUtils.CalculateMonkDc(monk);

        int damageAmount = hasOverflow switch
        {
            true => kiFocus switch
            {
                KiFocus.KiFocus1 => 40,
                KiFocus.KiFocus2 => 60,
                KiFocus.KiFocus3 => 80,
                _ => 20
            },
            false => kiFocus switch
            {
                KiFocus.KiFocus1 => 20,
                KiFocus.KiFocus2 => 30,
                KiFocus.KiFocus3 => 40,
                _ => 10
            }
        };

        DamageType damageType = hasOverflow switch
        {
            true => DamageType.Divine,
            false => DamageType.Negative
        };

        VfxType aoeVfx = hasOverflow switch
        {
            true => MonkVfx.ImpPulseHolyChest,
            false => MonkVfx.ImpPulseNegativeChest
        };

        Effect useFortVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);

        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(aoeVfx));

        foreach (NwCreature hostileCreature in monk.Location.GetObjectsInShapeByType<NwCreature>
                     (Shape.Sphere, RadiusSize.Large, false))
        {
            if (!monk.IsReactionTypeHostile(hostileCreature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, hostileCreature, NwSpell.FromSpellType(Spell.NegativeEnergyBurst)!);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                damageAmount /= 2;
                hostileCreature.ApplyEffect(EffectDuration.Instant, useFortVfx);
            }

            hostileCreature.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageAmount, damageType));
        }
    }
}
