using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public abstract class SpellDecorator : ISpell
{
    protected ISpell Spell;

    protected SpellDecorator(ISpell spell)
    {
        Spell = spell;
    }
    
    public virtual string ImpactScript => Spell.ImpactScript;
    
    public virtual void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        Spell.OnSpellImpact(eventData);
    }
    
}