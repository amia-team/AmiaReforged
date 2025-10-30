using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(WeakWildMagic))]
public class WeakWildMagic(WildMagicUtils wildMagicUtils)
{
    public void MagicMissile(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.MagicMissile);
        if (spell == null || target.Location == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Evocation, 1, monkLevel))
            return;

        Effect? magicMissileEffect = wildMagicUtils.MagicMissileEffect(monk, target.Location);
        if (magicMissileEffect == null) return;

        target.ApplyEffect(EffectDuration.Temporary, magicMissileEffect, TimeSpan.FromSeconds(0.4));
    }

    public void Flare(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Flare);
        if (spell is null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Evocation, 0, monkLevel))
            return;

        int diceAmount = monkLevel / 2;
        int damage = Random.Shared.Roll(3, diceAmount);

        Effect flare = Effect.Damage(damage, DamageType.Fire);
        flare.SubType = EffectSubType.Magical;

        _ = wildMagicUtils.GetObjectContext(monk, flare);

        target.ApplyEffect(EffectDuration.Instant, flare);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFlameS));
    }

    public void Bane(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Bane);
        if (spell == null || target.Location == null) return;

        Effect bane = Effect.LinkEffects
        (
            Effect.AttackDecrease(1),
            Effect.SavingThrowDecrease(SavingThrow.Will, 1, SavingThrowType.Fear),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        bane.SubType = EffectSubType.Magical;
        Effect baneHitVfx = Effect.VisualEffect(VfxType.ImpHeadEvil);

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfLosEvil30));

        foreach (NwCreature enemy in target.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, true))
        {
            if (!monk.IsReactionTypeHostile(enemy)) continue;

            if (wildMagicUtils.ResistedSpell(enemy, monk, spell, SpellSchool.Enchantment, 1, monkLevel))
                continue;

            SavingThrowResult savingThrowResult =
                enemy.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                enemy.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
                continue;
            }

            enemy.ApplyEffect(EffectDuration.Temporary, bane, WildMagicUtils.LongDuration);
            enemy.ApplyEffect(EffectDuration.Instant, baneHitVfx);
        }
    }

    public void Doom(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Doom);
        if (spell == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Enchantment, 1, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
            return;
        }

        Effect doom = Effect.LinkEffects
        (
            Effect.AttackDecrease(2),
            Effect.SavingThrowDecrease(SavingThrow.All, 2),
            Effect.SkillDecreaseAll(2),
            Effect.DamageDecrease(2),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        doom.SubType = EffectSubType.Magical;

        target.ApplyEffect(EffectDuration.Temporary, doom, WildMagicUtils.LongDuration);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDoom));
    }

    public void DeathArmor(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        monk.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.DeathArmorEffect(monk, monkLevel), WildMagicUtils.LongDuration);

    public void ElectricJolt(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.ElectricJolt);
        if (spell == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Evocation, 0, monkLevel))
            return;

        int diceAmount = monkLevel / 2;
        int damage = Random.Shared.Roll(3, diceAmount);

        Effect electricJolt = Effect.Damage(damage, DamageType.Electrical);
        electricJolt.SubType = EffectSubType.Magical;

        _ = wildMagicUtils.GetObjectContext(monk, electricJolt);

        target.ApplyEffect(EffectDuration.Instant, electricJolt);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpLightningS));
    }

    public void Silence(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Silence);
        if (spell == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Illusion, 2, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Spell, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
            return;
        }

        Effect silence = Effect.LinkEffects
        (
            Effect.Deaf(),
            Effect.Silence(),
            Effect.VisualEffect(VfxType.DurCessateNegative),
            Effect.DamageImmunityIncrease(DamageType.Sonic, 100)
        );
        silence.SubType = EffectSubType.Magical;

        target.ApplyEffect(EffectDuration.Temporary, silence, WildMagicUtils.LongDuration);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpSilence));
    }

    public void Combust(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Combust);
        if (spell == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Illusion, 2, monkLevel))
            return;

        target.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.CombustEffect(monk, target, monkLevel), WildMagicUtils.LongDuration);
    }

    public void CharmMonster(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.Combust);
        if (spell == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Enchantment, 3, monkLevel))
            return;

        SavingThrowResult savingThrowResult =
            target.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, WildMagicUtils.WillUseVfx);
            return;
        }

        target.ApplyEffect(EffectDuration.Temporary, Effect.Dazed(), WildMagicUtils.ShortDuration);
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpCharm));
    }

    public void InflictLightWounds(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.InflictLightWounds);
        if (spell == null) return;

        if (wildMagicUtils.ResistedSpell(target, monk, spell, SpellSchool.Necromancy, 1, monkLevel))
            return;

        target.ApplyEffect(EffectDuration.Instant, wildMagicUtils.InflictLightWoundsEffect(monk, monkLevel));
    }

    public void CureLightWounds(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        int bonusHeal = monkLevel > 5 ? 5 : monkLevel;
        int healRoll = Random.Shared.Roll(8);

        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingS));
        monk.ApplyEffect(EffectDuration.Instant, Effect.Heal(healRoll + bonusHeal));
    }

    public void LesserRestoration(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpRestorationLesser));

        foreach (Effect effect in monk.ActiveEffects)
        {
            if (effect.EffectType is EffectType.AbilityDecrease or EffectType.AcDecrease or EffectType.DamageDecrease
                    or EffectType.DamageImmunityDecrease or EffectType.SavingThrowDecrease or EffectType.SkillDecrease
                    or EffectType.AttackDecrease
                && effect.SubType != EffectSubType.Unyielding)

                monk.RemoveEffect(effect);
        }
    }

    public void ShelgarnsPersistentBlade(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        Effect summonBlade = Effect.SummonCreature("shelgarns_blade", VfxType.DurDeathArmor!);
        summonBlade.SubType = EffectSubType.Magical;

        _ = wildMagicUtils.GetObjectContext(monk, summonBlade);

        monk.ApplyEffect(EffectDuration.Temporary, summonBlade, WildMagicUtils.LongDuration);
    }
}
