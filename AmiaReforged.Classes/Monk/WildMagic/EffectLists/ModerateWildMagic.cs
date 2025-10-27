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
        if (target.Location == null) return;

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

    }

    public void InvisibilitySphere(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void CircleOfDeath(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void CureCriticalWounds(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void Restoration(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void PolymorphFoe(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void SoundBurst(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

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
