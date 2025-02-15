using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    string ImpactScript { get; }
    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
}