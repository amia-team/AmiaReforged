using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(ModerateWildMagic))]
public class ModerateWildMagic(WildMagicUtils wildMagicUtils)
{
    public void IsaacsLesserMissileStorm(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.IsaacsLesserMissileStorm);
        if (spell == null) return;
        if (target.Location == null) return;

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
            target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
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
            target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.None, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
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
        if (spell == null) return;
        if (monk.Location == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Transmutation, 2, monkLevel))
            return;

        monk.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfHowlWarCry));

        foreach (NwCreature enemy in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;
            if (Random.Shared.Roll(20) + enemy.GetAbilityModifier(Ability.Strength) >= Random.Shared.Roll(20) + 5)
                continue;

            _ = ApplyBalagarns(monk, enemy);
        }
    }

    private async Task ApplyBalagarns(NwCreature monk, NwCreature enemy)
    {
        float delay = monk.Distance(enemy) / 20;

        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        enemy.ApplyEffect(EffectDuration.Instant, Effect.Knockdown(), NwTimeSpan.FromRounds(1));
        enemy.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadNature));
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
            target.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            return;
        }

        target.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.RandomPolymorphEffect(), WildMagicUtils.LongDuration);
    }

    public void SoundBurst(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.SoundBurst);
        if (spell == null) return;
        if (target.Location == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Evocation, 2, monkLevel))
            return;

        Effect stun = Effect.Stunned();
        stun.SubType = EffectSubType.Magical;

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfSoundBurst));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Medium, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            Effect damage = Effect.Damage(Random.Shared.Roll(8), DamageType.Sonic);
            _ = wildMagicUtils.GetObjectContext(monk, damage);

            enemy.ApplyEffect(EffectDuration.Instant, damage);

            SavingThrowResult savingThrowResult =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, stun, WildMagicUtils.ShortDuration);
        }
    }

    public void MordenkainensSword(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void GedleesElectricLoop(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void BlindnessDeafness(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void Scare(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void HoldMonster(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }
}
