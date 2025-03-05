using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NUnit.Framework;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.CureMinorWounds;

[ServiceBinding(typeof(CureMinorWounds))]
public class CureMinorWounds : ISpell
{
    public ResistSpellResult Result { get; set; }
    public string ImpactScript => "NW_S0_CurMinW";
    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if(eventData.Caster == null) return;
        if(eventData.Caster is not NwCreature casterCreature) return;
        
        if(eventData.TargetObject == null) return;
        
        Task<TouchAttackResult> result = casterCreature.TouchAttackRanged(eventData.TargetObject, true);
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }
}