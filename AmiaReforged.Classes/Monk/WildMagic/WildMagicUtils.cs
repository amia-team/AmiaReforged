using Anvil.API;
using Anvil.Services;
using Npgsql.Internal;

namespace AmiaReforged.Classes.Monk.WildMagic;

[ServiceBinding(typeof(WildMagicUtils))]
public class WildMagicUtils(ScriptHandleFactory scriptHandleFactory)
{
    public bool CheckSpellResist(NwCreature target, NwCreature monk, NwSpell spell, SpellSchool school, int spellLevel, int monkLevel) =>
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

    private ScriptHandleResult ApplyCombust(NwCreature target, int monkLevel)
    {
        int damageRoll = Random.Shared.Roll(6, 2);
        int bonusDamage = monkLevel > 10 ? 10 : monkLevel;

        target.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageRoll + bonusDamage, DamageType.Fire));
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFlameM));

        return ScriptHandleResult.True;
    }
    private ScriptHandleResult DoCombust(NwCreature target)
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

    public Effect CharmMonsterEffect(NwCreature monk)
    {
        Effect charmMonster = Effect.LinkEffects
        (
            Effect.Charmed(),
            Effect.VisualEffect(VfxType.DurMindAffectingNegative)
        );
        charmMonster.SubType = EffectSubType.Magical;

        _ = GetObjectContext(monk, charmMonster);

        return charmMonster;
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

    private ScriptHandleResult ShootMissile(NwCreature monk, NwCreature[] enemies)
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

    private async Task ApplyMissileDamage(NwCreature monk, NwCreature target, float missileTravelDelay)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));

        int damageRoll = Random.Shared.Roll(6);

        await monk.WaitForObjectContext();
        Effect missileDamage = Effect.Damage(damageRoll);

        target.ApplyEffect(EffectDuration.Instant, missileDamage);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagblue, fScale: 0.7f));
    }

}
