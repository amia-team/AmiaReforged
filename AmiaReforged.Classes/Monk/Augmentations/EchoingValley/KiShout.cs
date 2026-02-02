using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.EchoingValley;

[ServiceBinding(typeof(IAugmentation))]
public class KiShout : IAugmentation.ICastAugment
{
    public PathType Path => PathType.EchoingValley;
    public TechniqueType Technique => TechniqueType.KiShout;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentKiShout(monk);
    }

    /// <summary>
    /// Ki Shout releases the monk's Echoes, each Echo exploding and dealing 10d6 sonic damage in a large radius.
    /// If the target succeeds on a fortitude save, they take half damage and avoid being stunned for 1 round.
    /// </summary>
    private void AugmentKiShout(NwCreature monk)
    {
        if (monk.Location == null) return;

        foreach (NwCreature echo in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Colossal, false))
        {
            if (echo.Master != monk || echo.ResRef != EchoConstant.SummonResRef) continue;

            _ = ExplodeEcho(monk, echo);
        }
    }

    private async Task ExplodeEcho(NwCreature monk, NwCreature echo)
    {
        float delay = monk.Distance(echo) / 10;
        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        Effect explosionVfx = MonkUtils.ResizedVfx(VfxType.FnfMysticalExplosion, RadiusSize.Large);
        if (echo.Location == null) return;

        echo.Location.ApplyEffect(EffectDuration.Instant, explosionVfx);

        foreach (NwCreature hostileCreature in echo.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, false))
        {
            if (!monk.IsReactionTypeHostile(hostileCreature)) continue;

            int dc = MonkUtils.CalculateMonkDc(monk);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Sonic, monk);

            int damageAmount = Random.Shared.Roll(6, 10);

            switch (savingThrowResult)
            {
                case SavingThrowResult.Success:
                    damageAmount /= 2;
                    hostileCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                    break;
                case SavingThrowResult.Failure:
                    hostileCreature.ApplyEffect(EffectDuration.Temporary, Effect.Stunned(), NwTimeSpan.FromRounds(1));
                    break;
            }

            await echo.WaitForObjectContext();
            Effect damageEffect = Effect.LinkEffects(
                Effect.Damage(damageAmount, DamageType.Sonic),
                Effect.VisualEffect(VfxType.ImpSonic)
            );

            hostileCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        }

        echo.Destroy();
    }
}
