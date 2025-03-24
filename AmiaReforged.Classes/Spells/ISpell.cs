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
        ResistedSpell = creature.SpellAbsorptionLimitedCheck(caster)
                        || creature.SpellAbsorptionUnlimitedCheck(caster)
                        || creature.SpellImmunityCheck(caster)
                        || creature.SpellResistanceCheck(caster);
        
        CheckedSpellResistance = true;
    }

    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
    void SetSpellResisted(bool result);
}