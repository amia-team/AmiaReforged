using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public abstract class SpellDecorator : ISpell
{
    protected ISpell Spell;

    protected SpellDecorator(ISpell spell)
    {
        Spell = spell;
    }

    public bool ResistedSpell { get; set; }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster);
        Spell.SetSpellResisted(ResistedSpell);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
        Spell.SetSpellResisted(ResistedSpell);
    }

    public virtual string ImpactScript => Spell.ImpactScript;

    public virtual void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        Spell.OnSpellImpact(eventData);
    }
}