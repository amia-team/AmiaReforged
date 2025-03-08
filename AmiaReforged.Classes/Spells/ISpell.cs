using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    ResistSpellResult Result { get; set; }
    string ImpactScript { get; }
    void DoSpellResist(NwCreature creature, NwCreature caster);
    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
    void SetSpellResistResult(ResistSpellResult result);
}