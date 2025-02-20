using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    ResistSpellResult Result { get; set; }
    void DoSpellResist(NwCreature creature, NwCreature caster);
    string ImpactScript { get; }
    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
}