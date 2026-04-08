using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class LightsCalling : IInvocation
{
    private const string CelestialSummonResRef = "wlkCelestial";

    public string ImpactScript => "wlk_lightscall";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        int dc = warlock.InvocationDc(invocationCl);

        Effect blindness = Effect.LinkEffects(Effect.Blindness(), Effect.VisualEffect(VfxType.DurCessateNegative));
        blindness.SubType = EffectSubType.Magical;

        Effect turned = Effect.LinkEffects(Effect.Turned(), Effect.VisualEffect(VfxType.DurMindAffectingFear));
        turned.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(1);

        Effect willVfx = Effect.VisualEffect(VfxType.ImpWillSavingThrowUse);
        Effect impVfx = Effect.VisualEffect(VfxType.ImpSunstrike);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSunbeam));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(
                     Shape.Sphere, RadiusSize.Colossal, losCheck: false))
        {
            if (!creature.IsValidInvocationTarget(warlock, false)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, castData.Spell);

            if (warlock.InvocationResistCheck(creature, invocationCl)) continue;

            SavingThrowResult willSave = creature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Good, warlock);

            if (willSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, willVfx);
                continue;
            }

            _ = ApplyLight(creature, willSave, willVfx, impVfx, blindness, turned, duration);

            if (creature.Race.RacialType == RacialType.Undead)
            {
                creature.ApplyEffect(EffectDuration.Temporary, turned, duration);
                continue;
            }

            creature.ApplyEffect(EffectDuration.Temporary, blindness, duration);
        }

        if (warlock.ActiveEffects.Any(e => e.Tag == WarlockExtensions.PactSummonCooldownTag))
            return;

        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(invocationCl);

        Effect summonCelestial = Effect.SummonCreature(CelestialSummonResRef, VfxType.ImpFearS!, appearType: 1);
        location.ApplyEffect(EffectDuration.Temporary, summonCelestial, summonDuration);

        warlock.ApplyPactCooldown();
    }

    private static async Task ApplyLight(NwCreature creature, SavingThrowResult willSave, Effect willVfx, Effect impVfx,
        Effect blindness, Effect turned, TimeSpan duration)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(0.8, 1.3));

        if (willSave == SavingThrowResult.Success)
        {
            creature.ApplyEffect(EffectDuration.Instant, willVfx);
            return;
        }

        creature.ApplyEffect(EffectDuration.Instant, impVfx);

        if (creature.Race.RacialType == RacialType.Undead)
        {
            creature.ApplyEffect(EffectDuration.Temporary, turned, duration);
            return;
        }

        creature.ApplyEffect(EffectDuration.Temporary, blindness, duration);
    }
}
