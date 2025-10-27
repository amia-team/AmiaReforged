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
