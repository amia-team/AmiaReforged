using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class SpellContext
{
    public NwGameObject? CasterObject { get; set; }
    public NwGameObject? TargetObject { get; set; }

    public NwCreature? CasterCreature { get; }
    public NwCreature? TargetCreature { get; }


    public SpellEvents.OnSpellCast EventData { get; }
    
    public SpellContext(SpellEvents.OnSpellCast eventData)
    {
        EventData = eventData;
        
        CasterObject = eventData.Caster;
        TargetObject = eventData.TargetObject;

        if (CasterObject is NwCreature creature)
        {
            CasterCreature = creature;
        }
        
        if (TargetObject is NwCreature target)
        {
            TargetCreature = target;
        }
    }
}