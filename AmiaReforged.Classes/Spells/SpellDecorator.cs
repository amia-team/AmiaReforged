using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public abstract class SpellDecorator : ISpell
{
    protected ISpell Spell;
    public ResistSpellResult Result { get; set; }

    protected SpellDecorator(ISpell spell)
    {
        Spell = spell;
    }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
        Spell.SetSpellResistResult(Result);
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
        Spell.SetSpellResistResult(Result);
    }

    public virtual string ImpactScript => Spell.ImpactScript;

    public virtual void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        Spell.OnSpellImpact(eventData);
    }
}