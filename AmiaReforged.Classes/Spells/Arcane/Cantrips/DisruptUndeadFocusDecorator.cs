using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

[DecoratesSpell(typeof(DisruptUndead))]
public class DisruptUndeadFocusDecorator : SpellDecorator
{
    public DisruptUndeadFocusDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        
        Spell.OnSpellImpact(eventData);
    }
}