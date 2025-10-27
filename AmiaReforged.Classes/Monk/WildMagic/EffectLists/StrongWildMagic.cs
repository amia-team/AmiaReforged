using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(StrongWildMagic))]
public class StrongWildMagic(WildMagicUtils wildMagicUtils)
{
    public void IsaacsGreaterMissileStorm(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        NwSpell? spell = NwSpell.FromSpellType(Spell.IsaacsGreaterMissileStorm);
        if (spell == null) return;
        if (target.Location == null) return;

        if (wildMagicUtils.CheckSpellResist(target, monk, spell, SpellSchool.Evocation, 6, monkLevel))
            return;

        Effect? magicMissileEffect = wildMagicUtils.MagicMissileEffect(monk, target.Location);
        if (magicMissileEffect == null) return;

        target.ApplyEffect(EffectDuration.Temporary, magicMissileEffect, TimeSpan.FromSeconds(0.18));
    }

    public void Web(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void GustOfWind(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void Confusion(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void NegativeEnergyBurst(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void CallLightning(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void ScintillatingSphere(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void Fireball(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void Slow(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void GreaterPlanarBinding(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void BigbysInterposingHand(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void MassBlindnessDeafness(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }

    public void MassPolymorph(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {

    }
}
