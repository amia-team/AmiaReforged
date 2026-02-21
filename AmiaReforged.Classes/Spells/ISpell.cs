using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public interface ISpell
{
    string ImpactScript { get; }

    void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        if (CheckedSpellResistance) return;
        ResistedSpell = caster.SpellAbsorptionLimitedCheck(creature)
                        || caster.SpellAbsorptionUnlimitedCheck(creature)
                        || caster.SpellImmunityCheck(creature)
                        || caster.SpellResistanceCheck(creature);

        CheckedSpellResistance = true;
    }

    void OnSpellImpact(SpellEvents.OnSpellCast eventData);
    void SetSpellResisted(bool result);
    bool CheckedSpellResistance { get; set; }
    bool ResistedSpell { get; set; }
}
