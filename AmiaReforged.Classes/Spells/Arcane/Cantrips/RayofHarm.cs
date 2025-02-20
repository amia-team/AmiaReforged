using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips;

[ServiceBinding(typeof(RayofHarm))]
public class RayofHarm : ISpell
{
    public string ImpactScript => "am_s_rayofharm";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        throw new NotImplementedException();
    }

    public ResistSpellResult Result { get; set; }
    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }
}