using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.NinthCircle.Necromancy;

/**
 * Innate level: 9
 * School: necromancy
 * Descriptor: death, sonic
 * Components: verbal
 * Range: short
 * Area of effect: 10 meter radius
 * Duration: instant
 * Save: fortitude negates
 * Spell resistance: yes
 *
 * Description: You emit a terrible, soul-chilling scream that potentially kills creatures that hear it.
 * The spell can kill up to 1 creature per caster level. Creatures closest to the point of origin are
 * affected first. Pale Master levels add to caster level and reduce target spell resistance.
 *
 * Amia: Targets take 5% of their max health as negative energy damage per necromancy spell focus tier,
 * even if they pass their save. Undead are healed instead. Death immunity negates the effect.
 */
[ServiceBinding(typeof(ISpell))]
public class WailOfTheBanshee(DeathSpellService deathSpellService) : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_WailBansh";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;
        if (eventData.TargetLocation == null) return;

        int paleMasterLevel = caster.GetClassInfo(ClassType.PaleMaster)?.Level ?? 0;
        int casterLevel = caster.CasterLevel + paleMasterLevel;
        int maxTargets = casterLevel;
        int dc = eventData.SaveDC;

        Effect wailVfx = Effect.VisualEffect(VfxType.FnfWailOBanshees);
        Effect deathVfx = Effect.VisualEffect(VfxType.ImpDeath);
        Effect death = Effect.Death();
        death.SubType = EffectSubType.Magical;

        // Apply the FNF VFX impact at the target location
        eventData.TargetLocation.ApplyEffect(EffectDuration.Instant, wailVfx);

        // Get hostile creatures sorted by distance from the spell target location
        List<NwCreature> targets = GetTargetsByDistance(eventData, caster, maxTargets);

        int targetCount = 0;
        foreach (NwCreature target in targets)
        {
            if (targetCount >= maxTargets) break;

            // Signal spell cast at event
            CreatureEvents.OnSpellCastAt.Signal(caster, target, eventData.Spell);

            // Apply temporary spell resistance decrease based on Pale Master level
            if (paleMasterLevel > 0)
            {
                Effect srDecrease = Effect.SpellResistanceDecrease(paleMasterLevel);
                target.ApplyEffect(EffectDuration.Temporary, srDecrease, TimeSpan.FromSeconds(0.5));
            }

            // Check spell resistance
            if (SpellUtils.MyResistSpell(caster, target))
            {
                targetCount++;
                continue;
            }

            // Apply death effect (with save) and percentage-based damage (no save, with delay)
            _ = ApplyDeathEffect(caster, target, dc, deathVfx, death);
            _ = ApplyDelayedDeathSpellDamage(caster, target);

            targetCount++;
        }
    }

    private async Task ApplyDelayedDeathSpellDamage(NwCreature caster, NwCreature target)
    {
        TimeSpan delay = SpellUtils.GetRandomDelay(3.0, 4.0);
        await NwTask.Delay(delay);

        await caster.WaitForObjectContext();

        deathSpellService.ApplyDeathSpellDamage(caster, target);
    }

    private static List<NwCreature> GetTargetsByDistance(SpellEvents.OnSpellCast eventData, NwCreature caster,
        int maxTargets)
    {
        if (eventData.TargetLocation is not { } targetLocation) return [];
        List<(NwCreature creature, float distance)> targetsByDistance = [];

        targetsByDistance.AddRange(
            targetLocation
                .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, false)
                .Where(creature => caster.IsReactionTypeHostile(creature) && !creature.IsDead)
                .Select(creature => (
                    creature,
                    distance: Vector3.Distance(targetLocation.Position, creature.Position)
                ))
        );

        // Sort by distance and take up to maxTargets
        return targetsByDistance
            .OrderBy(t => t.distance)
            .Take(maxTargets)
            .Select(t => t.creature)
            .ToList();
    }

    private static async Task ApplyDeathEffect(NwCreature caster, NwCreature target, int dc, Effect deathVfx,
        Effect death)
    {
        TimeSpan delay = SpellUtils.GetRandomDelay(3.0, 4.0);
        await NwTask.Delay(delay);

        SavingThrowResult saveResult = target.RollSavingThrow(
            SavingThrow.Fortitude,
            dc,
            SavingThrowType.Death,
            caster);

        if (saveResult == SavingThrowResult.Success)
            return;

        await caster.WaitForObjectContext();

        target.ApplyEffect(EffectDuration.Instant, deathVfx);
        target.ApplyEffect(EffectDuration.Instant, death);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
