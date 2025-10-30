using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(ModerateWildMagic))]
public class ModerateWildMagic(WildMagicUtils wildMagicUtils)
{
    public void IsaacsLesserMissileStorm(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.IsaacsLesserMissileStorm);
        if (spell == null || target.Location == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Evocation, 4, monkLevel))
            return;

        Effect? magicMissileEffect = wildMagicUtils.MagicMissileEffect(monk, target.Location);
        if (magicMissileEffect == null) return;

        target.ApplyEffect(EffectDuration.Temporary, magicMissileEffect, TimeSpan.FromSeconds(0.8));
    }

    public void HealingSting(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.HealingSting);
        if (spell == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Necromancy, 3, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, monk);

        int damage = Random.Shared.Roll(6) + monkLevel;
        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.FortUseVfx);
            damage /= 2;
        }

        target.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Negative));
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpNegativeEnergy));
        monk.ApplyEffect(EffectDuration.Instant, Effect.Heal(damage));
        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingM));
    }

    public void InflictCriticalWounds(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        int damage = Math.Min((int)monkLevel, 20) + Random.Shared.Roll(8, 4);

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Negative, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
            damage /= 2;
        }

        Effect inflict = Effect.Damage(damage, DamageType.Negative);
        _ = wildMagicUtils.GetObjectContext(monk, inflict);

        target.ApplyEffect(EffectDuration.Instant, inflict);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHarm));
    }

    public void Concealment(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        monk.ApplyEffect(EffectDuration.Temporary, Effect.Concealment(50), WildMagicUtils.LongDuration);

    public void BalagarnsIronHorn(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Balagarnsironhorn);
        if (spell == null || monk.Location == null) return;

        Effect knockdown = Effect.Knockdown();
        Effect impHeadNature = Effect.VisualEffect(VfxType.ImpHeadNature);

        monk.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfHowlWarCry));

        foreach (NwCreature enemy in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Transmutation, 2, monkLevel))
                continue;

            if (Random.Shared.Roll(20) + enemy.GetAbilityModifier(Ability.Strength) >= Random.Shared.Roll(20) + 5)
                continue;

            _ = ApplyBalagarns(enemy);
        }

        return;

        async Task ApplyBalagarns(NwCreature enemy)
        {
            float delay = monk.Distance(enemy) / 20;

            await NwTask.Delay(TimeSpan.FromSeconds(delay));

            enemy.ApplyEffect(EffectDuration.Instant, knockdown, NwTimeSpan.FromRounds(1));
            enemy.ApplyEffect(EffectDuration.Instant, impHeadNature);
        }
    }

    public void CureCriticalWounds(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        int heal = Math.Min((int)monkLevel, 20) + Random.Shared.Roll(8, 4);

        monk.ApplyEffect(EffectDuration.Instant, Effect.Heal(heal));
        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingG));
    }

    public void Restoration(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpRestoration));

        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.EffectType is EffectType.AbilityDecrease or EffectType.AcDecrease or EffectType.DamageDecrease
                    or EffectType.DamageImmunityDecrease or EffectType.SavingThrowDecrease or EffectType.SkillDecrease
                    or EffectType.Blindness or EffectType.Deaf or EffectType.Paralyze or EffectType.NegativeLevel or
                    EffectType.AttackDecrease
                && effect.SubType != EffectSubType.Unyielding)

                monk.RemoveEffect(effect);
        }
    }

    public void BalefulPolymorph(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Feeblemind);
        if (spell == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Transmutation, 5, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Spell, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.FortUseVfx);
            return;
        }

        target.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.RandomPolymorphEffect(), WildMagicUtils.LongDuration);
    }

    public void SoundBurst(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.SoundBurst);
        if (spell == null || target.Location == null) return;

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSoundBurst));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Medium, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Evocation, 2, monkLevel))
                continue;

            Effect damage = Effect.Damage(Random.Shared.Roll(8), DamageType.Sonic);
            _ = wildMagicUtils.GetObjectContext(monk, damage);

            enemy.ApplyEffect(EffectDuration.Instant, damage);

            SavingThrowResult savingThrowResult =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, WildMagicUtils.Stun, WildMagicUtils.ShortDuration);
        }
    }

    public void MordenkainensSword(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        Effect summonHelmHorror = Effect.SummonCreature("NW_S_HelmHorr", VfxType.FnfSummonMonster3!);
        summonHelmHorror.SubType = EffectSubType.Magical;

        _ = wildMagicUtils.GetObjectContext(monk, summonHelmHorror);

        monk.ApplyEffect(EffectDuration.Temporary, summonHelmHorror, WildMagicUtils.LongDuration);
    }

    public void GedleesElectricLoop(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.GedleesElectricLoop);
        if (spell == null || target.Location == null) return;

        Effect lightningVfx = Effect.VisualEffect(VfxType.ImpLightningS);

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Small,
                     true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.CheckSpellResist(enemy, monk, spell, SpellSchool.Evocation, 2, monkLevel))
                continue;

            SavingThrowResult reflexSaveResult =
                enemy.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Electricity, monk);

            if (reflexSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.ReflexUseVfx);

                if (enemy.KnowsFeat(Feat.Evasion!) || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                    continue;
            }

            int damage = Random.Shared.Roll(6, 5);

            if (reflexSaveResult == SavingThrowResult.Success || enemy.KnowsFeat(Feat.ImprovedEvasion!))
                damage /= 2;

            Effect damageEffect = Effect.Damage(damage, DamageType.Electrical);
            _ = wildMagicUtils.GetObjectContext(monk, damageEffect);

            enemy.ApplyEffect(EffectDuration.Instant, damageEffect);
            enemy.ApplyEffect(EffectDuration.Instant, lightningVfx);

            if (reflexSaveResult == SavingThrowResult.Success) continue;

            SavingThrowResult willSaveResult =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Electricity, monk);

            if (willSaveResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, WildMagicUtils.Stun);
        }
    }

    public void BlindnessDeafness(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.BlindnessAndDeafness);
        if (spell == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Enchantment, 2, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Spell, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.FortUseVfx);
            return;
        }

        target.ApplyEffect(EffectDuration.Temporary, WildMagicUtils.BlindnessDeafnessEffect, WildMagicUtils.ShortDuration);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpBlindDeafM));
    }

    public void HoldMonster(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.HoldMonster);
        if (spell == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Enchantment, 4, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
            return;
        }

        target.ApplyEffect(EffectDuration.Temporary, Effect.Paralyze(), WildMagicUtils.ShortDuration);
        target.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.DurParalyzeHold), WildMagicUtils.ShortDuration);
    }
}
