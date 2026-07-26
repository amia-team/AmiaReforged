using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.MagicMissile;

[ServiceBinding(typeof(MagicMissileService))]
public class MagicMissileService(ScriptHandleFactory scriptHandleFactory)
{
    /// <summary>
    /// Builds the missile target queue and applies one timed effect to the caster.
    /// Each effect callback consumes one queued target and fires one missile.
    /// Spell resistance checks and targeting are handled internally.
    /// </summary>
    /// <param name="caster">Caster of the missiles</param>
    /// <param name="location">Location where the spell was targeted</param>
    /// <param name="spell">Spell type from the OnSpellCast event data</param>
    /// <param name="metaMagic">Metamagic from the OnSpellCast event data.</param>
    public void ApplyMissiles(NwCreature caster, Location location, NwSpell spell, MetaMagic metaMagic)
    {
        NwCreature[] missileTargets = GetMissileTargets(caster, spell.SpellType, location);

        foreach (NwCreature target in missileTargets)
            SpellUtils.SignalSpell(caster, target, spell);

        if (missileTargets.Length == 0)
            return;

        MissileData? missileData = GetMissileData(caster, spell);
        if (missileData == null) return;

        Effect missileEffect = MagicMissileEffect(caster, missileTargets, missileData, metaMagic);
        caster.ApplyEffect(EffectDuration.Temporary, missileEffect, GetMissileDuration(missileData.MissileCount));
    }

    /// <summary>
    /// Builds the missile target queue and applies one timed effect to the caster.
    /// Each effect callback consumes one queued target and fires one missile.
    /// </summary>
    /// <param name="caster">Caster of the missiles</param>
    /// <param name="target">Missile target</param>
    /// <param name="spell">Spell type from the OnSpellCast event data</param>
    /// <param name="metaMagic">Metamagic from the OnSpellCast event data.</param>
    public void ApplyMissiles(NwCreature caster, NwGameObject target, NwSpell spell, MetaMagic metaMagic)
    {
        MissileData? missileData = GetMissileData(caster, spell);
        if (missileData == null) return;
        SpellUtils.SignalSpell(caster, target, spell);
        Effect missileEffect = MagicMissileEffect(caster, target, missileData, metaMagic);
        caster.ApplyEffect(EffectDuration.Temporary, missileEffect, GetMissileDuration(missileData.MissileCount));
    }

    /// <summary>
    /// Expands valid targets into a per-missile target queue according to the spell's targeting rules.
    /// </summary>
    private static NwCreature[] GetMissileTargets(NwCreature caster, Spell spellType, Location location)
    {
        int missileCount = GetMissileCount(caster.CasterLevel, spellType);

        NwCreature[] targets = location
            .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Gargantuan, true)
            .Where(c => caster.IsReactionTypeHostile(c) && caster.IsCreatureSeen(c))
            .Take(missileCount)
            .ToArray();

        if (targets.Length == 0)
            return [];

        return spellType switch
        {
            Spell.IsaacsLesserMissileStorm => GetRandomMissileTargets(targets, missileCount),
            Spell.IsaacsGreaterMissileStorm => GetCappedMissileTargets(targets, missileCount, missileCap: 10),
            _ => []
        };
    }

    private static NwCreature[] GetRandomMissileTargets(NwCreature[] targets, int missileCount)
    {
        if (targets.Length == 0 || missileCount <= 0)
            return [];

        NwCreature[] missileTargets = new NwCreature[missileCount];

        for (int i = 0; i < missileTargets.Length; i++)
            missileTargets[i] = targets[Random.Shared.Next(targets.Length)];

        return missileTargets;
    }

    private static NwCreature[] GetCappedMissileTargets(NwCreature[] targets, int missileCount, int missileCap)
    {
        if (targets.Length == 0 || missileCount <= 0)
            return [];

        List<NwCreature> missileTargetPool = new(targets.Length * missileCap);

        foreach (NwCreature target in targets)
        {
            for (int i = 0; i < missileCap; i++)
                missileTargetPool.Add(target);
        }

        int targetCount = Math.Min(missileCount, missileTargetPool.Count);
        NwCreature[] cappedMissileTargets = new NwCreature[targetCount];

        for (int i = 0; i < cappedMissileTargets.Length; i++)
        {
            int targetIndex = Random.Shared.Next(missileTargetPool.Count);

            cappedMissileTargets[i] = missileTargetPool[targetIndex];

            missileTargetPool[targetIndex] = missileTargetPool[^1];
            missileTargetPool.RemoveAt(missileTargetPool.Count - 1);
        }

        return cappedMissileTargets;
    }

    private static TimeSpan GetMissileDuration(int missileCount) => TimeSpan.FromSeconds(missileCount * 0.1);

    private record MissileData(
        NwSpell Spell,
        int DiceSides,
        int DiceAmount,
        int FlatDamage,
        int DamageBonus,
        int MissileCount,
        VfxType MirvVfx,
        VfxType[] ImpVfx,
        DamageType[] DamageTypes);

    /// <summary>
    /// Creates a timed effect where on-apply, each interval, and on-remove fire a single missile.
    /// The target array is treated as a queue: one entry equals one missile.
    /// </summary>
    private Effect MagicMissileEffect(NwCreature caster, NwCreature[] targets, MissileData missileData, MetaMagic metaMagic)
    {
        int missileIndex = 0;

        ScriptCallbackHandle shootMissile
            = scriptHandleFactory.CreateUniqueHandler(_ =>
            {
                if (missileIndex >= targets.Length)
                    return ScriptHandleResult.False;

                NwGameObject target = targets[missileIndex++];
                return ShootMissile(caster, target, metaMagic, missileData);
            });

        Effect magicMissileEffect = Effect.RunAction(
            onAppliedHandle: shootMissile,
            onIntervalHandle: shootMissile,
            interval: TimeSpan.FromSeconds(0.1f));

        magicMissileEffect.SubType = EffectSubType.Magical;

        return magicMissileEffect;
    }

    private Effect MagicMissileEffect(NwCreature caster, NwGameObject target, MissileData missileData, MetaMagic metaMagic)
    {
        ScriptCallbackHandle shootMissile
            = scriptHandleFactory.CreateUniqueHandler(_ => ShootMissile(caster, target, metaMagic, missileData));

        Effect magicMissileEffect = Effect.RunAction(
            onAppliedHandle: shootMissile,
            onIntervalHandle: shootMissile,
            interval: TimeSpan.FromSeconds(0.1f));

        magicMissileEffect.SubType = EffectSubType.Magical;

        return magicMissileEffect;
    }

    private static ScriptHandleResult ShootMissile(
        NwCreature caster,
        NwGameObject target,
        MetaMagic metaMagic,
        MissileData missileData)
    {
        float distanceToTarget = caster.Distance(target);
        float missileTravelDelay = distanceToTarget / (3f * float.Log(distanceToTarget) + 2f);

        ResistSpellResult resistSpellResult
            = caster.MyResistSpell(target, missileData.Spell, feedback: false, playVisuals: false);

        int damage = 0;
        if (resistSpellResult == ResistSpellResult.Failed) damage = CalculateMissileDamage(metaMagic, missileData);
        _ = ApplyMissile(caster, target, missileTravelDelay, damage, missileData, resistSpellResult);

        return ScriptHandleResult.True;
    }

    private static int CalculateMissileDamage(MetaMagic metaMagic, MissileData missileData)
    {
        int damageRoll = SpellUtils.MaximizeSpell(metaMagic, missileData.DiceSides, missileData.DiceAmount)
                         + missileData.FlatDamage;

        int damage = damageRoll + missileData.DamageBonus;
        damage = SpellUtils.EmpowerSpell(metaMagic, damage);

        return damage;
    }

    private static async Task ApplyMissile(NwCreature caster, NwGameObject target, float missileTravelDelay, int damage,
        MissileData missileData, ResistSpellResult resistSpellResult)
    {
        await caster.WaitForObjectContext();
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(missileData.MirvVfx));

        await NwTask.Delay(TimeSpan.FromSeconds(missileTravelDelay));
        await caster.WaitForObjectContext();

        switch (resistSpellResult)
        {
            case ResistSpellResult.Resisted:
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagicResistanceUse));
                return;
            case ResistSpellResult.ResistedMagicImmune:
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
                return;
            case ResistSpellResult.ResistedSpellAbsorbed:
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpSpellMantleUse));
                return;
        }

        foreach (DamageType damageType in missileData.DamageTypes)
            target.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, damageType));

        foreach (VfxType vfxType in missileData.ImpVfx)
            target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfxType));
    }

    private static MissileData? GetMissileData(NwCreature caster, NwSpell? spell)
    {
        if (spell == null) return null;

        int damageBonus = GetDamageBonus(caster, spell.SpellType);
        int missileCount = GetMissileCount(caster.CasterLevel, spell.SpellType);

        return spell.SpellType switch
        {
            Spell.MagicMissile => new MissileData(
                Spell: spell,
                DiceSides: 4,
                DiceAmount: 1,
                FlatDamage: 1,
                DamageBonus: damageBonus,
                MissileCount: missileCount,
                MirvVfx: VfxType.ImpMirv,
                ImpVfx: [VfxType.ImpMagblue],
                DamageTypes: [DamageType.Magical]),

            Spell.ShadowConjurationMagicMissile => new MissileData(
                Spell: spell,
                DiceSides: 4,
                DiceAmount: 1,
                FlatDamage: 1,
                DamageBonus: damageBonus,
                MissileCount: missileCount,
                MirvVfx: AmiaVfxTypes.ImpMirvNegative,
                ImpVfx: [VfxType.ComHitNegative, VfxType.ImpFrostS],
                DamageTypes: [DamageType.Cold, DamageType.Negative]),

            Spell.IsaacsLesserMissileStorm => new MissileData(
                spell,
                DiceSides: 6,
                DiceAmount: 1,
                FlatDamage: 0,
                DamageBonus: damageBonus,
                MissileCount: missileCount,
                MirvVfx: VfxType.ImpMirv,
                ImpVfx: [VfxType.ImpMagblue],
                DamageTypes: [DamageType.Magical]),

            Spell.IsaacsGreaterMissileStorm => new MissileData(
                spell,
                DiceSides: 6,
                DiceAmount: 2,
                FlatDamage: 0,
                DamageBonus: damageBonus,
                MissileCount: missileCount,
                MirvVfx: VfxType.ImpMirv,
                ImpVfx: [VfxType.ImpMagblue],
                DamageTypes: [DamageType.Magical]),

            _=> null
        };
    }

    private static int GetDamageBonus(NwCreature caster, Spell spell)
        => spell switch
        {
            Spell.MagicMissile => caster.KnowsFeat(Feat.EpicSpellFocusEvocation!) ? 3 :
                caster.KnowsFeat(Feat.GreaterSpellFocusEvocation!) ? 2 :
                caster.KnowsFeat(Feat.SpellFocusEvocation!) ? 1 : 0,

            Spell.ShadowConjurationMagicMissile => caster.KnowsFeat(Feat.EpicSpellFocusIllusion!) ? 3 :
                caster.KnowsFeat(Feat.GreaterSpellFocusIllusion!) ? 2 :
                caster.KnowsFeat(Feat.SpellFocusIllusion!) ? 1 : 0,

            _ => 0
        };

    private static int GetMissileCount(int casterLevel, Spell spell)
        => spell switch
        {
            Spell.MagicMissile or Spell.ShadowConjurationMagicMissile => Math.Min((casterLevel - 1) / 2 + 1, 5),
            Spell.IsaacsLesserMissileStorm => Math.Min(casterLevel, 10),
            Spell.IsaacsGreaterMissileStorm => Math.Min(casterLevel, 30),
            _ => 0
        };
}
