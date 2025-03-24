﻿using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public abstract class SpellDecorator : ISpell
{
    protected ISpell Spell;

    public bool CheckedSpellResistance { get; set; }

    protected SpellDecorator(ISpell spell)
    {
        Spell = spell;
    }

    public bool ResistedSpell { get; set; }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        if (CheckedSpellResistance) return;
        ResistedSpell = creature.SpellAbsorptionLimitedCheck(caster)
                        || creature.SpellAbsorptionUnlimitedCheck(caster)
                        || creature.SpellImmunityCheck(caster)
                        || creature.SpellResistanceCheck(caster);
        
        Spell.SetSpellResisted(ResistedSpell);
        Spell.CheckedSpellResistance = true;
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
        Spell.SetSpellResisted(ResistedSpell);
        Spell.CheckedSpellResistance = true;
    }

    public virtual string ImpactScript => Spell.ImpactScript;

    public virtual void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        Spell.OnSpellImpact(eventData);
    }
}