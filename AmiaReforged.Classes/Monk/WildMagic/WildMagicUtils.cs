using Anvil.API;
using Anvil.Services;
using Npgsql.Internal;

namespace AmiaReforged.Classes.Monk.WildMagic;

[ServiceBinding(typeof(WildMagicUtils))]
public class WildMagicUtils(ScriptHandleFactory scriptHandleFactory)
{
    public bool ResistedSpell(NwCreature target, NwCreature monk, NwSpell spell, SpellSchool school, int spellLevel, int monkLevel) =>
        monk.SpellAbsorptionLimitedCheck(target, spell, school, spellLevel)
        || monk.SpellAbsorptionUnlimitedCheck(target, spell, school, spellLevel)
        || monk.SpellResistanceCheck(target, spell, monkLevel);

    public async Task GetObjectContext(NwCreature monk, Effect effect)
    {
        await monk.WaitForObjectContext();
        Effect awaitedEffect = effect;
    }

    public Effect RandomPolymorphEffect()
    {
        int randomRoll = Random.Shared.Roll(6);

        PolymorphType? polymorphType = randomRoll switch
        {
            1 => PolymorphType.Chicken,
            2 => PolymorphType.Cow,
            3 => PolymorphType.Penguin,
            4 => PolymorphType.Pixie,
            5 => PolymorphType.Quasit,
            6 => PolymorphType.Troll,
            _ => null
        };

        Effect randomPolymorph = Effect.LinkEffects
        (
            Effect.Polymorph(polymorphType!, false, VfxType.FnfSummonMonster1),
            Effect.TemporaryHitpoints(100)
        );

        randomPolymorph.SubType = EffectSubType.Magical;

        return randomPolymorph;
    }

    public static TimeSpan ShortDuration => NwTimeSpan.FromRounds(2);

    public static TimeSpan LongDuration => NwTimeSpan.FromRounds(5);

    public Effect CombustEffect(NwCreature monk, NwCreature target, byte monkLevel)
    {
        ScriptCallbackHandle applyCombust
            = scriptHandleFactory.CreateUniqueHandler(_ => ApplyCombust(target, monkLevel));

        ScriptCallbackHandle doCombust
            = scriptHandleFactory.CreateUniqueHandler(_ => DoCombust(target));

        Effect combustEffect = Effect.LinkEffects
        (
            Effect.RunAction(applyCombust, onIntervalHandle: doCombust, interval: NwTimeSpan.FromRounds(1)),
            Effect.VisualEffect(VfxType.DurInfernoChest)
        );
        combustEffect.SubType = EffectSubType.Magical;

        _ = GetObjectContext(monk, combustEffect);

        return combustEffect;
    }

    private static ScriptHandleResult ApplyCombust(NwCreature target, int monkLevel)
    {
        int damageRoll = Random.Shared.Roll(6, 2);
        int bonusDamage = monkLevel > 10 ? 10 : monkLevel;

        target.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageRoll + bonusDamage, DamageType.Fire));
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFlameM));

        return ScriptHandleResult.True;
    }
    private static ScriptHandleResult DoCombust(NwCreature target)
    {
        int damageRoll = Random.Shared.Roll(6);
        target.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageRoll, DamageType.Fire));
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFlameS));

        return ScriptHandleResult.True;
    }

    public Effect DeathArmorEffect(NwCreature monk, byte monkLevel)
    {
        int bonusDamage = monkLevel / 2 > 5 ? 5 : monkLevel / 2;
        int damageRoll = Random.Shared.Roll(4);

        Effect deathArmor = Effect.LinkEffects
        (
            Effect.VisualEffect(VfxType.DurDeathArmor),
            Effect.DamageShield(damageRoll + bonusDamage, 0, DamageType.Magical)
        );
        deathArmor.SubType = EffectSubType.Magical;

        _ = GetObjectContext(monk, deathArmor);

        return deathArmor;
    }

    public Effect InflictLightWoundsEffect(NwCreature monk, byte monkLevel)
    {
        int bonusDamage = monkLevel > 5 ? 5 : monkLevel;
        int damageRoll = Random.Shared.Roll(8);

        Effect inflictLightWounds = Effect.LinkEffects
        (
            Effect.Damage(damageRoll + bonusDamage, DamageType.Negative),
            Effect.VisualEffect(VfxType.ComHitNegative)
        );
        inflictLightWounds.SubType = EffectSubType.Magical;

        _ = GetObjectContext(monk, inflictLightWounds);

        return inflictLightWounds;
    }

    public Effect? MagicMissileEffect(NwCreature monk, Location targetLocation)
    {
        NwCreature[] enemies = targetLocation
            .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Gargantuan, true)
            .Where(monk.IsReactionTypeHostile)
            .ToArray();

        if (enemies.Length == 0) return null;

        ScriptCallbackHandle shootMissile
            = scriptHandleFactory.CreateUniqueHandler(_ => ShootMissile(monk,enemies));

        Effect magicMissileEffect = Effect.RunAction(shootMissile,
            shootMissile, shootMissile, TimeSpan.FromSeconds(0.1f));

        return magicMissileEffect;
    }

    private static ScriptHandleResult ShootMissile(NwCreature monk, NwCreature[] enemies)
    {
        Random random = new();

        enemies = enemies.Where(c => !c.IsDead).ToArray();

        if (enemies.Length == 0) return ScriptHandleResult.False;

        NwCreature randomEnemy = enemies[random.Next(enemies.Length)];

        float distanceToTarget = monk.Distance(randomEnemy);
        float missileTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 2f);

        randomEnemy.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMirv));
        _ = ApplyMissileDamage(monk, randomEnemy,  missileTravelDelay);

        return ScriptHandleResult.True;
    }

    private static async Task ApplyMissileDamage(NwCreature monk, NwCreature target, float missileTravelDelay)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));

        int damageRoll = Random.Shared.Roll(6);

        await monk.WaitForObjectContext();
        Effect missileDamage = Effect.Damage(damageRoll);

        target.ApplyEffect(EffectDuration.Instant, missileDamage);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagblue, fScale: 0.7f));
    }

    public Effect? RunTimeStopEffect(NwCreature monk)
    {
        if (monk.Location == null) return null;
        monk.Location.ApplyEffect(EffectDuration.Instant, TimeStopVfx);

        NwCreature[] creatures = monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
            RadiusSize.Colossal * 3, false)
            .Where(c => !c.IsDMPossessed && c != monk)
            .ToArray();

        if (creatures.Length == 0) return null;

        Effect[] damageImmunities =
            Enum.GetValues<DamageType>().Select(damageType => Effect.DamageImmunityIncrease(damageType, 100)).ToArray();

        Effect timeStopEffect = damageImmunities[0];

        for (int i = 1; i < damageImmunities.Length; i++)
        {
            timeStopEffect = Effect.LinkEffects(timeStopEffect, damageImmunities[i]);
        }

        timeStopEffect = Effect.LinkEffects
        (
            timeStopEffect,
            Effect.Immunity(ImmunityType.None),
            Effect.SpellImmunity(),
            Effect.CutsceneParalyze(),
            Effect.VisualEffect(VfxType.DurFreezeAnimation)
        );

        timeStopEffect.SubType = EffectSubType.Supernatural;

        ScriptCallbackHandle timeStopStart
            = scriptHandleFactory.CreateUniqueHandler(_ => TimeStopStart(creatures, timeStopEffect));

        ScriptCallbackHandle timeStopEnd
            = scriptHandleFactory.CreateUniqueHandler(_ => TimeStopEnd(monk, creatures));

        Effect runTimeStopEffect = Effect.RunAction(timeStopStart,
            timeStopEnd);

        timeStopStart.Dispose();
        timeStopEnd.Dispose();

        return runTimeStopEffect;
    }

    private static ScriptHandleResult TimeStopStart(NwCreature[] creatures, Effect timeStopEffect)
    {
        foreach (NwCreature creature in creatures)
        {
            creature.ApplyEffect(EffectDuration.Temporary, timeStopEffect, TimeSpan.FromSeconds(9));
            creature.ApplyEffect(EffectDuration.Instant, TimeStopStartVfx);
        }

        return ScriptHandleResult.True;
    }

    private static ScriptHandleResult TimeStopEnd(NwCreature monk, NwCreature[] creatures)
    {
        monk.ApplyEffect(EffectDuration.Instant, TimeStopVfx);

        foreach (NwCreature creature in creatures)
            creature.ApplyEffect(EffectDuration.Instant, TimeStopEndVfx);

        return ScriptHandleResult.True;
    }


    private static readonly Random Random = new();

    /// <summary>
    /// Generates a random double within the specified range.
    /// </summary>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <returns>A random double between min (inclusive) and max (exclusive).</returns>
    public static double GetRandomDoubleInRange(double min, double max)
    {
        if (min > max)
        {
            return 0;
        }

        double randomDouble = Random.NextDouble();
        return min + randomDouble * (max - min);
    }

    private static readonly Effect TimeStopStartVfx = Effect.VisualEffect((VfxType)471);
    private static readonly Effect TimeStopEndVfx = Effect.VisualEffect((VfxType)471);
    private static readonly Effect TimeStopVfx = Effect.VisualEffect(VfxType.FnfTimeStop);

    public static readonly Effect BlindnessDeafnessEffect =
        Effect.LinkEffects(Effect.LinkEffects(Effect.Blindness(), Effect.Deaf()));
    public static readonly Effect WillUseVfx = Effect.VisualEffect(VfxType.ImpWillSavingThrowUse);
    public static readonly Effect ReflexUseVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
    public static readonly Effect FortUseVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);
    public static readonly Effect Stun = Effect.Stunned();
    public static readonly Effect Hold = Effect.Paralyze();
}
