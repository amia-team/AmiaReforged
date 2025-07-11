using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Ranger;

[ServiceBinding(typeof(EpicCompanionHandler))]
public class EpicCompanionHandler
{
    private const string EpicCompanionAppearance = "epic_companion_appearance";
    private const string PcKeyTag = "ds_pckey";
    
    /// <summary>
    /// This script opts in and out of the Epic Companion appearance. Rest of the functionality is handled by
    /// nw_s2_animalcom; we should bring that to C# at some point!
    /// </summary>
    [ScriptHandler(scriptName: "epic_ac")]
    public void OnEpicCompanion(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature creature) return;
        
        NwPlayer? player = creature.ControllingPlayer;
        if (player == null) return;
        
        NwItem? pcKey = creature.FindItemWithTag(PcKeyTag);
        if (pcKey == null) return;
        

        LocalVariableInt epicCompanionAppearance = pcKey.GetObjectVariable<LocalVariableInt>(EpicCompanionAppearance);
        
        switch (epicCompanionAppearance.Value)
        {
            case 0:
                epicCompanionAppearance.Value = 1;
                player.SendServerMessage("Opted in for the Epic Companion appearance."); 
                break;
            
            case 1:
            {
                epicCompanionAppearance.Delete();
                player.SendServerMessage("Opted out of the Epic Companion appearance.");
                break;
            }
        }
    }
}