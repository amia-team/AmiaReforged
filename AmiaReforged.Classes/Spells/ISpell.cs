using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    bool CheckedSpellResistance { get; set; }
    bool ResistedSpell { get; set; }
    string ImpactScript { get; }

    void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        if (CheckedSpellResistance) return;
        creature.SpeakString("Checking spell resistance.");
        ResistedSpell = creature.SpellResistanceCheck(caster)
                        || creature.SpellImmunityCheck(caster)
                        || creature.SpellAbsorptionLimitedCheck(caster)
                        || creature.SpellAbsorptionUnlimitedCheck(caster);
        CheckedSpellResistance = true;
    }

    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
    void SetSpellResisted(bool result);
}