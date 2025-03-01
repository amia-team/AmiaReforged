using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(CantripRenewalService))]
public class CantripRenewalService
{

    public CantripRenewalService()
    {
        NwModule.Instance.OnClientEnter += OnClientEnter;
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        NwPlayer player = obj.Player;
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        CreatureClassInfo? wizard = creature.Classes.SingleOrDefault(c => c.Class.ClassType == ClassType.Wizard);

        if (wizard is null) return;

        Dictionary<string, int> allNewCantrips = new()
        {
            { "am_s_rayofharm", 1039},
            {"am_s_disruptun", 1040},
            {"amx_csp_bsound", 867}
        };
        
        foreach (string scriptResRef in allNewCantrips.Keys)
        {
            if(wizard.KnownSpells[0].All(s => s.ImpactScript != scriptResRef))
            {
                CreaturePlugin.AddKnownSpell(creature, wizard.Class.Id, 1, allNewCantrips[scriptResRef]);
            }
        }
    }
}