using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class LoudDecay : IInvocation
{
    private const VfxType FnfLoudDecay = (VfxType)2133;
    private const VfxType ImpDestructLow = (VfxType)302;
    private const string AberrationSummonResRef = "wlkaberrant";

    public string ImpactScript => "wlk_louddecay";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        int dc = warlock.InvocationDc(invocationCl);
        int damageDice = invocationCl / 2;
        const int dieSides = 6;

        Effect damageVfx = Effect.VisualEffect(VfxType.ImpSonic);
        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingM);
        Effect fortVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(FnfLoudDecay));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>
                     (Shape.Sphere, RadiusSize.Colossal, losCheck: false))
        {
            if (creature.ResRef == AberrationSummonResRef)
            {
                int healAmount = Random.Shared.Roll(dieSides, damageDice) / 2;
                _ = HealSummon(warlock, creature, healVfx, healAmount);
                continue;
            }

            if (!creature.IsValidInvocationTarget(warlock)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, castData.Spell);

            if (warlock.InvocationResistCheck(creature, invocationCl)) continue;

            int damageAmount = Random.Shared.Roll(dieSides, damageDice);

            SavingThrowResult fortSave =
                creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Spell, warlock);

            if (fortSave == SavingThrowResult.Success)
            {
                damageAmount /= 2;
                creature.ApplyEffect(EffectDuration.Instant, fortVfx);
            }

            _ = ApplyDamage(warlock, creature, damageVfx, damageAmount);
        }

        if (warlock.ActiveEffects.Any(e => e.Tag == WarlockExtensions.PactSummonCooldownTag)) return;

        int summonCount = invocationCl switch
        {
            >= 1 and < 15 => 1,
            >= 15 and < 30 => 2,
            >= 30 => 3,
            _ => 0
        };

        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(invocationCl);
        VisualEffectTableEntry summonVfx = NwGameTables.VisualEffectTable.GetRow((int)VfxType.FnfGasExplosionNature);
        VisualEffectTableEntry unsummonVfx = NwGameTables.VisualEffectTable.GetRow((int)ImpDestructLow);

        Effect summonEffect = Effect.SummonCreature(AberrationSummonResRef, summonVfx, unsummonVfx: unsummonVfx);
        summonEffect.SubType = EffectSubType.Magical;

        location.SummonMany(warlock, summonCount, RadiusSize.Gargantuan, delayMin: 1f, delayMax: 2f, summonEffect,
            summonDuration);

        warlock.ApplyPactCooldown();
    }

    private static async Task HealSummon(NwCreature warlock, NwCreature creature, Effect healVfx, int healAmount)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(1, 2));
        await warlock.WaitForObjectContext();

        creature.ApplyEffect(EffectDuration.Instant, healVfx);
        creature.ApplyEffect(EffectDuration.Instant, Effect.Heal(healAmount));
    }

    private static async Task ApplyDamage(NwCreature warlock, NwCreature creature, Effect damageVfx, int damageAmount)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(3));
        await warlock.WaitForObjectContext();

        creature.ApplyEffect(EffectDuration.Instant, damageVfx);
        creature.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageAmount, DamageType.Sonic));
    }
}
