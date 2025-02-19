using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public abstract class SpellDecorator : ISpell
{
    protected readonly ISpell _spell;

    protected SpellDecorator(ISpell spell)
    {
        _spell = spell;
    }
    
    public virtual string ImpactScript => _spell.ImpactScript;
    
    public virtual void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        _spell.OnSpellImpact(eventData);
    }
    
}