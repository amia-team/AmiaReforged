using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(EpicWildMagic))]
public class EpicWildMagic(WildMagicUtils wildMagicUtils)
{
    public void TimeStop(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.TimeStop);
        if (spell == null) return;

        Effect? runTimeStopEffect = wildMagicUtils.RunTimeStopEffect(monk);
        if (runTimeStopEffect == null) return;

        monk.ApplyEffect(EffectDuration.Temporary, runTimeStopEffect, TimeSpan.FromSeconds(9));
    }

    public void GreatThunderclap(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.GreatThunderclap);
        if (spell == null || target.Location == null) return;

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfMysticalExplosion));
        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfScreenShake));

        Effect knockdown = Effect.Knockdown();
        Effect deaf = Effect.Deaf();
        Effect stun = Effect.Stunned();

        TimeSpan roundDuration = NwTimeSpan.FromRounds(1);
        TimeSpan turnDuration = NwTimeSpan.FromTurns(1);

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Gargantuan, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;
            if (wildMagicUtils.ResistedSpell(enemy, monk, spell, SpellSchool.Evocation, 7, monkLevel))
                continue;

            SavingThrowResult willSavingThrow =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Sonic, monk);
            SavingThrowResult fortSavingThrow =
                enemy.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Sonic, monk);
            SavingThrowResult reflexSavingThrow =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Sonic, monk);

            if (willSavingThrow == SavingThrowResult.Failure)
                enemy.ApplyEffect(EffectDuration.Temporary, stun, roundDuration);

            if (fortSavingThrow == SavingThrowResult.Failure)
                enemy.ApplyEffect(EffectDuration.Temporary, deaf, turnDuration);

            if (reflexSavingThrow == SavingThrowResult.Failure)
                enemy.ApplyEffect(EffectDuration.Temporary, knockdown, roundDuration);
        }
    }

    public void HammerOfTheGods(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.HammerOfTheGods);
        if (spell == null || target.Location == null) return;

        Effect divineVfx = Effect.VisualEffect(VfxType.ImpDivineStrikeHoly);
        Effect daze = Effect.LinkEffects(Effect.Dazed(), Effect.VisualEffect(VfxType.DurMindAffectingNegative));

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfStrikeHoly));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.ResistedSpell(enemy, monk, spell, SpellSchool.Evocation, 4, monkLevel))
                continue;

            SavingThrowResult willSaveResult =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Divine, monk);

            int damage = Random.Shared.Roll(8, 5);


            enemy.ApplyEffect(EffectDuration.Instant, divineVfx);
            Effect damageEffect;
            switch (willSaveResult)
            {
                case SavingThrowResult.Success:
                    damage /= 2;
                    damageEffect = Effect.Damage(damage, DamageType.Divine);
                    _ = wildMagicUtils.GetObjectContext(monk, damageEffect);
                    enemy.ApplyEffect(EffectDuration.Instant, damageEffect);
                    continue;
                case SavingThrowResult.Failure:
                    damageEffect = Effect.Damage(damage, DamageType.Divine);
                    _ = wildMagicUtils.GetObjectContext(monk, damageEffect);
                    enemy.ApplyEffect(EffectDuration.Instant, damageEffect);
                    enemy.ApplyEffect(EffectDuration.Temporary, daze, WildMagicUtils.ShortDuration);
                    continue;
            }
        }
    }

    public void Firestorm(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.FireStorm);
        if (spell == null || monk.Location == null) return;

        Effect fireVfx = Effect.VisualEffect(VfxType.ImpFlameM);

        monk.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfFirestorm));

        foreach (NwCreature enemy in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.ResistedSpell(enemy, monk, spell, SpellSchool.Evocation, 7, monkLevel))
                continue;

            SavingThrowResult reflexSaveResult =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Fire, monk);

            bool fireDamageHit = true;
            if (reflexSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.ReflexUseVfx);

                if (enemy.KnowsFeat(Feat.Evasion!) || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                    fireDamageHit = false;
            }

            int divineDamage = Random.Shared.Roll(6, 10);

            int? fireDamage = null;
            if (fireDamageHit)
            {
                fireDamage = divineDamage;
                if (reflexSaveResult == SavingThrowResult.Success || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                    fireDamage /= 2;
            }

            _ = ApplyFireStorm(enemy, divineDamage, fireDamage);
        }

        return;

        async Task ApplyFireStorm(NwCreature enemy, int divineDamage, int? fireDamage)
        {
            double randomDelay = WildMagicUtils.GetRandomDoubleInRange(1.5, 2.5);

            await NwTask.Delay(TimeSpan.FromSeconds(randomDelay));

            enemy.ApplyEffect(EffectDuration.Instant, fireVfx);

            await monk.WaitForObjectContext();
            Effect divineDamageEffect = Effect.Damage(divineDamage, DamageType.Divine);
            enemy.ApplyEffect(EffectDuration.Instant, divineDamageEffect);
            enemy.ApplyEffect(EffectDuration.Instant, fireVfx);

            if (fireDamage != null)
            {
                await monk.WaitForObjectContext();
                Effect fireDamageEffect = Effect.Damage(divineDamage, DamageType.Fire);
                enemy.ApplyEffect(EffectDuration.Instant, fireDamageEffect);
            }
        }
    }

    public void MeteorSwam(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.MeteorSwarm);
        if (spell == null || target.Location == null) return;

        Effect fireVfx = Effect.VisualEffect(VfxType.ImpFlameM);

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfMeteorSwarm));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.ResistedSpell(enemy, monk, spell, SpellSchool.Evocation, 9, monkLevel))
                continue;

            SavingThrowResult reflexSaveResult =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Fire, monk);

            if (reflexSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.ReflexUseVfx);

                if (enemy.KnowsFeat(Feat.Evasion!) || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                    continue;
            }

            int damage = Random.Shared.Roll(4, 20);

            if (reflexSaveResult == SavingThrowResult.Success || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                damage /= 2;

            _ = ApplyMeteorSwarm(enemy, damage);
        }

        return;

        async Task ApplyMeteorSwarm(NwCreature enemy, int damage)
        {
            double randomDelay = WildMagicUtils.GetRandomDoubleInRange(1, 1.5);
            await NwTask.Delay(TimeSpan.FromSeconds(randomDelay));


            await monk.WaitForObjectContext();
            Effect meteorDamageEffect = Effect.LinkEffects(Effect.Damage(damage, DamageType.Fire),
                Effect.Damage(damage, DamageType.Bludgeoning));

            enemy.ApplyEffect(EffectDuration.Instant, meteorDamageEffect);
            enemy.ApplyEffect(EffectDuration.Instant, fireVfx);
        }
    }

    public void BlackBladeOfDisaster(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        Effect summonBlackBlade = Effect.SummonCreature("the_black_blade", VfxType.DurDeathArmor!);
        summonBlackBlade.SubType = EffectSubType.Magical;

        _ = wildMagicUtils.GetObjectContext(monk, summonBlackBlade);

        monk.ApplyEffect(EffectDuration.Temporary, summonBlackBlade, WildMagicUtils.LongDuration);

    }
}
