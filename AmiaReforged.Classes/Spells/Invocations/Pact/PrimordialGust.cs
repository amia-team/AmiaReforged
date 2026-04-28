using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;
using static AmiaReforged.Classes.Warlock.PactSummon.Elemental.ElementalSummonData;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class PrimordialGust : IInvocation
{
    public string ImpactScript => "wlk_primordial";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        Effect impVfx = Effect.LinkEffects(Effect.VisualEffect(VfxType.ImpFrostS),
            Effect.VisualEffect(VfxType.ImpFlameS), Effect.VisualEffect(VfxType.ImpLightningS));
        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingS);

        int dc = warlock.InvocationDc(invocationCl);
        int damageDice = invocationCl / 3;

        foreach (NwGameObject target in location.GetObjectsInShape(Shape.SpellCone, size: 11f, losCheck: true,
                     ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door, origin: warlock.Position))
        {
            int damage = Random.Shared.Roll(4, damageDice);

            if (target is NwDoor or NwPlaceable)
            {
                _ = ApplyPrimordialDamage(warlock, target, damage, impVfx);
                continue;
            }

            if (target is not NwCreature creature)
                continue;

            if (IsMephit(creature))
            {
                _ = HealSummon(warlock, creature, healVfx, damage);
                continue;
            }

            if (!creature.IsValidInvocationTarget(warlock, hurtSelf: false))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, castData.Spell);

            if (warlock.InvocationResistCheck(creature, invocationCl))
                continue;

            bool hasEvasion = creature.KnowsFeat(Feat.Evasion!);
            bool hasImpEvasion = creature.KnowsFeat(Feat.ImprovedEvasion!);

            SavingThrowResult reflexSave = creature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.None, warlock);

            if (reflexSave == SavingThrowResult.Success)
            {
                target.ApplyEffect(EffectDuration.Instant, reflexVfx);

                if (hasEvasion || hasImpEvasion)
                    continue;

                damage /= 2;
                _ = ApplyPrimordialDamage(warlock, target, damage, impVfx);
                continue;
            }

            if (hasImpEvasion) damage /= 2;

            _ = ApplyPrimordialDamage(warlock, target, damage, impVfx);
        }

        if (warlock.HasPactCooldown()) return;

        int summonCount = invocationCl switch
        {
            >= 1 and < 15 => 1,
            >= 15 and < 30 => 2,
            >= 30 => 3,
            _ => 0
        };

        string[] summonResRefs = [FireMephit, WaterMephit, SteamMephit];
        VfxType[] summonVfx = [VfxType.ImpFlameM, VfxType.ImpDispel, VfxType.ImpElementalProtection];
        Effect[] summonEffects = new Effect[summonCount];
        for (int i = 0; i < summonCount; i++)
        {
            summonEffects[i] = Effect.SummonCreature(summonResRefs[i], summonVfx[i]!, unsummonVfx: summonVfx[i]);
        }

        location.SummonManyDifferent(
            warlock,
            summonEffects,
            RadiusSize.Small,
            delayMin: 0.6f,
            delayMax: 1f,
            summonDuration: WarlockExtensions.PactSummonDuration(invocationCl));

        warlock.ApplyPactCooldown();
    }

    private static bool IsMephit(NwCreature creature) => creature.ResRef.StartsWith(FireMephit);

    private static async Task ApplyPrimordialDamage(NwCreature warlock, NwGameObject target, int damage, Effect impVfx)
    {
        TimeSpan delay = TimeSpan.FromSeconds(warlock.Distance(target) / 16);
        await NwTask.Delay(delay);

        DamageData primordialDamageType = new()
        {
            iCold = damage,
            iFire = damage,
            iElectrical = damage
        };

        DamagePlugin.DealDamage(primordialDamageType, target, oSource: warlock);
        target.ApplyEffect(EffectDuration.Instant, impVfx);
    }

    private static async Task HealSummon(NwCreature warlock, NwCreature creature, Effect healVfx, int healAmount)
    {
        TimeSpan delay = TimeSpan.FromSeconds(warlock.Distance(creature) / 16);
        await NwTask.Delay(delay);
        await warlock.WaitForObjectContext();

        creature.ApplyEffect(EffectDuration.Instant, healVfx);
        creature.ApplyEffect(EffectDuration.Instant, Effect.Heal(healAmount));
    }
}
