using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class LightsCalling : IInvocation
{
    private const string CelestialSummonResRef = "wlkcelestial";

    public string ImpactScript => "wlk_lightscall";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        int dc = warlock.InvocationDc(invocationCl);

        Effect blindness = Effect.LinkEffects(Effect.Blindness(), Effect.VisualEffect(VfxType.DurCessateNegative));
        blindness.SubType = EffectSubType.Magical;

        Effect turned = Effect.LinkEffects(Effect.Turned(), Effect.VisualEffect(VfxType.DurMindAffectingFear));
        turned.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl / 10);

        Effect fortVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);
        Effect willVfx = Effect.VisualEffect(VfxType.ImpWillSavingThrowUse);
        Effect impVfx = Effect.VisualEffect(VfxType.ImpSunstrike);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSunbeam));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(
                     Shape.Sphere, RadiusSize.Colossal, losCheck: false))
        {
            if (!creature.IsValidInvocationTarget(warlock, false)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, castData.Spell);

            if (warlock.InvocationResistCheck(creature, invocationCl)) continue;

            _ = ApplyLight(creature, warlock, dc, fortVfx, willVfx, impVfx, blindness, turned, duration);
        }

        if (warlock.HasPactCooldown()) return;

        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(invocationCl);

        Effect summonCelestial = Effect.SummonCreature(CelestialSummonResRef, VfxType.ImpFearS!, appearType: 1);
        location.ApplyEffect(EffectDuration.Temporary, summonCelestial, summonDuration);

        warlock.ApplyPactCooldown();
    }

    private static async Task ApplyLight(NwCreature creature, NwCreature warlock, int dc, Effect fortVfx,
        Effect willVfx, Effect impVfx, Effect blindness, Effect turned, TimeSpan duration)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(0.8, 1.3));
        await warlock.WaitForObjectContext();

        if (creature.IsDead || !creature.IsValid) return;

        bool isUndead = creature.Race.RacialType == RacialType.Undead;

        SavingThrowResult saveResult;
        if (isUndead)
        {
            saveResult = creature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.None, warlock);
        }
        else
        {
            saveResult = creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, warlock);
            creature.ApplyEffect(EffectDuration.Instant, fortVfx);
        }

        if (saveResult == SavingThrowResult.Success)
        {
            creature.ApplyEffect(EffectDuration.Instant, isUndead ? willVfx : fortVfx);
            return;
        }

        creature.ApplyEffect(EffectDuration.Instant, impVfx);
        creature.ApplyEffect(EffectDuration.Temporary, isUndead ? turned : blindness, duration);
    }
}
