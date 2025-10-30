using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(StrongWildMagic))]
public class StrongWildMagic(WildMagicUtils wildMagicUtils)
{
    public void IsaacsGreaterMissileStorm(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.IsaacsGreaterMissileStorm);
        if (spell == null || target.Location == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Evocation, 6, monkLevel))
            return;

        Effect? magicMissileEffect = wildMagicUtils.MagicMissileEffect(monk, target.Location);
        if (magicMissileEffect == null) return;

        target.ApplyEffect(EffectDuration.Temporary, magicMissileEffect, TimeSpan.FromSeconds(0.18));
    }

    public static void Web(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Web);
        if (spell == null || target.Location == null) return;

        target.Location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.DurWebMass));

        Effect web = Effect.LinkEffects(Effect.Entangle(), Effect.VisualEffect(VfxType.DurWeb));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            SavingThrowResult savingThrowResult =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Spell, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.ReflexUseVfx);
                enemy.ApplyEffect(EffectDuration.Temporary, Effect.MovementSpeedDecrease(50), WildMagicUtils.ShortDuration);

                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, web, WildMagicUtils.ShortDuration);
        }
    }

    public static void GustOfWind(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.GustOfWind);
        if (spell == null || target.Location == null) return;

        Effect knockdown = Effect.Knockdown();

        target.Location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.FnfLosNormal20));

        foreach (NwGameObject nwObject in target.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Huge, true))
        {
            if (nwObject is NwDoor nwDoor)
            {
                if (nwDoor.IsOpen) nwDoor.Close();
                else if (nwDoor is { Locked: false, IsOpen: false }) nwDoor.Open();

                continue;
            }

            if (nwObject is NwAreaOfEffect { Spell: not null } aoe)
            {
                aoe.Destroy();
                continue;
            }

            if (nwObject is not NwCreature enemy || !monk.IsReactionTypeHostile(enemy)) continue;

            SavingThrowResult fortSaveResult =
                enemy.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, monk);

            if (fortSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.FortUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, knockdown, WildMagicUtils.ShortDuration);
        }
    }

    public void Confusion(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Confusion);
        if (spell == null || target.Location == null) return;

        Effect confusion = Effect.LinkEffects(Effect.Confused(), Effect.VisualEffect(VfxType.DurMindAffectingDisabled));
        Effect confusionImp = Effect.VisualEffect(VfxType.ImpConfusionS);

        target.Location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.FnfLosNormal20));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large,
                     true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Enchantment, 3, monkLevel))
                continue;

            SavingThrowResult willSaveResult =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

            if (willSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, confusion, WildMagicUtils.ShortDuration);
            enemy.ApplyEffect(EffectDuration.Instant, confusionImp);
        }
    }

    public void NegativeEnergyBurst(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.NegativeEnergyBurst);
        if (spell == null || target.Location == null) return;

        int damage = Math.Min((int)monkLevel, 20) + Random.Shared.Roll(8);

        Effect strDrain = Effect.LinkEffects(Effect.AbilityDecrease(Ability.Strength, 3),
            Effect.VisualEffect(VfxType.DurCessateNegative));

        Effect damageEffect = Effect.LinkEffects(Effect.VisualEffect(VfxType.ImpNegativeEnergy),
            Effect.Damage(damage, DamageType.Negative));
        _ = wildMagicUtils.GetObjectContext(monk, damageEffect);

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPulseNegative));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large,
                     true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Necromancy, 3, monkLevel))
                continue;

            enemy.ApplyEffect(EffectDuration.Temporary, strDrain, WildMagicUtils.LongDuration);
            enemy.ApplyEffect(EffectDuration.Instant, damageEffect);
        }
    }

    public void CallLightning(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.CallLightning);
        if (spell == null || target.Location == null) return;

        Effect lightningVfx = Effect.VisualEffect(VfxType.ImpLightningM);

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Evocation, 3, monkLevel))
                continue;

            SavingThrowResult reflexSaveResult =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Electricity, monk);

            if (reflexSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.ReflexUseVfx);

                if (enemy.KnowsFeat(Feat.Evasion!) || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                    continue;
            }

            int damage = Random.Shared.Roll(6, 10);

            if (reflexSaveResult == SavingThrowResult.Success || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                damage /= 2;

            Effect damageEffect = Effect.Damage(damage, DamageType.Electrical);
            _ = wildMagicUtils.GetObjectContext(monk, damageEffect);

            enemy.ApplyEffect(EffectDuration.Instant, damageEffect);
            enemy.ApplyEffect(EffectDuration.Instant, lightningVfx);
        }
    }

    public void Fireball(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Fireball);
        if (spell == null || target.Location == null) return;

        Effect fireVfx = Effect.VisualEffect(VfxType.ImpFlameM);

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfFireball));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Evocation, 3, monkLevel))
                continue;

            SavingThrowResult reflexSaveResult =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Fire, monk);

            if (reflexSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.ReflexUseVfx);

                if (enemy.KnowsFeat(Feat.Evasion!) || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                    continue;
            }

            int damage = Random.Shared.Roll(6, 10);

            if (reflexSaveResult == SavingThrowResult.Success || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                damage /= 2;

            Effect damageEffect = Effect.Damage(damage, DamageType.Electrical);
            _ = wildMagicUtils.GetObjectContext(monk, damageEffect);

            enemy.ApplyEffect(EffectDuration.Instant, damageEffect);
            enemy.ApplyEffect(EffectDuration.Instant, fireVfx);
        }
    }

    public void Slow(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Slow);
        if (spell == null || target.Location == null) return;

        Effect slow = Effect.LinkEffects(Effect.Slow(), Effect.VisualEffect(VfxType.DurCessateNegative));
        Effect slowImp = Effect.VisualEffect(VfxType.ImpSlow);

        target.Location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.FnfLosNormal30));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal,
                     true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Transmutation, 3, monkLevel))
                continue;

            SavingThrowResult willSaveResult = enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Spell, monk);

            if (willSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, slow, WildMagicUtils.ShortDuration);
            enemy.ApplyEffect(EffectDuration.Instant, slowImp);
        }
    }

    public void GreaterPlanarBinding(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.GreaterPlanarBinding);
        if (spell == null) return;

        string prefix = "planar_3_";

        int outcome = Random.Shared.Roll(3);

        string suffix = outcome switch
        {
            1 => "g",
            2 => "n",
            3 => "e",
            _ => "g"
        };

        VfxType summonVfx = suffix switch
        {
            "g" => VfxType.FnfSummonCelestial,
            "n" => VfxType.FnfSummonMonster3,
            "e" => VfxType.FnfSummonGate,
            _ => VfxType.FnfSummonCelestial
        };

        if (suffix == "e")
        {
            int evilOutcome = Random.Shared.Roll(3);

            suffix = evilOutcome switch
            {
                1 => "le",
                2 => "ne",
                3 => "ce",
                _ => "le"
            };
        }

        string planarResRef = prefix + suffix;

        Effect summonPlanar = Effect.SummonCreature(planarResRef, summonVfx!, TimeSpan.FromSeconds(1));
        _ = wildMagicUtils.GetObjectContext(monk, summonPlanar);

        monk.ApplyEffect(EffectDuration.Temporary, summonPlanar, WildMagicUtils.LongDuration);
    }

    public void BigbysInterposingHand(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.BigbysInterposingHand);
        if (spell == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Evocation, 5, monkLevel))
            return;

        Effect interposingHand = Effect.LinkEffects(Effect.VisualEffect(VfxType.DurBigbysInterposingHand), Effect.AttackDecrease(10));
        interposingHand.SubType = EffectSubType.Magical;

        target.ApplyEffect(EffectDuration.Temporary, interposingHand, WildMagicUtils.LongDuration);
    }

    public void MassBlindnessDeafness(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.MassBlindnessAndDeafness);
        if (spell == null || target.Location == null) return;

        Effect blindDeafVfx = Effect.VisualEffect(VfxType.ImpBlindDeafM);

        target.Location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.FnfBlinddeaf));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large,
                     true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Illusion, 8, monkLevel))
                continue;

            SavingThrowResult fortSaveResult = enemy.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Spell, monk);

            if (fortSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.FortUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, WildMagicUtils.BlindnessDeafnessEffect, WildMagicUtils.ShortDuration);
            enemy.ApplyEffect(EffectDuration.Instant, blindDeafVfx);
        }
    }

    public void MassPolymorph(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Feeblemind);
        if (spell == null) return;
        if (target.Location == null) return;

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSummonMonster1, fScale: 1.5f));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Transmutation, 8, monkLevel))
                continue;

            SavingThrowResult savingThrowResult =
                enemy.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Instant, wildMagicUtils.RandomPolymorphEffect(), WildMagicUtils.LongDuration);
        }
    }
}
