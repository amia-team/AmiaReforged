using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    bool ResistedSpell { get; set; }
    string ImpactScript { get; }

    void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster)
                        || creature.SpellImmunityCheck(caster)
                        || creature.SpellAbsorptionLimitedCheck(caster)
                        || creature.SpellAbsorptionUnlimitedCheck(caster);
    }

    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
    void SetSpellResisted(bool result);
}