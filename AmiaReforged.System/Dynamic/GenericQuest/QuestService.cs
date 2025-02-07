using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Dynamic.GenericQuest;

[ServiceBinding(typeof(QuestService))]
public class QuestService
{
    // Gets the server log. By default, this reports to "anvil.log"
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// When script ee_generic_quest is called, handle all generic quest-related functionality........
    /// </summary>
    // [ScriptHandler("ee_generic_quest")]
    private static void OnScriptCalled(CallInfo callInfo)
    {
        Log.Info($"ee_generic_quest called by {callInfo.ObjectSelf?.Name}");
        
        CreatureEvents.OnConversation eventData = new();
        
        // START VARIABLE DECLARATION

        NwCreature playerCharacter = eventData.PlayerSpeaker!.ControlledCreature!;
        NwCreature questGiver = eventData.Creature;
        
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");
            
        // Indexing for the quest variables starts at 1 for legacy reasons; non-indexed quest variables like "questname"
        // are treated as "questname1"
        
        int index = 1;

        // The next three local vars must be found on any quest, so we give debug messages if they're not provided!
        
        LocalVariableString questName = questGiver.GetObjectVariable<LocalVariableString>("questname"+index);
        if (questName.HasNothing)
        {
            questName = questGiver.GetObjectVariable<LocalVariableString>("questname");
            if (questName.HasNothing) playerCharacter.LoginPlayer!.SendServerMessage("DEBUG: No quest name found.");
        }
        
        LocalVariableString speechWithQuest = questGiver.GetObjectVariable<LocalVariableString>("speechwithquest"+index);
        if (speechWithQuest.HasNothing)
        {
            speechWithQuest = questGiver.GetObjectVariable<LocalVariableString>("speechwithquest");
            if (speechWithQuest.HasNothing) playerCharacter.LoginPlayer!.SendServerMessage("DEBUG: No speech with quest found.");
        }
        
        LocalVariableString speechQuestDone = questGiver.GetObjectVariable<LocalVariableString>("speechquestdone"+index);
        if (speechQuestDone.HasNothing)
        {
            speechQuestDone = questGiver.GetObjectVariable<LocalVariableString>("speechquestdone");
            if (speechQuestDone.HasNothing) playerCharacter.LoginPlayer!.SendServerMessage("DEBUG: No speech quest done found.");
        }
        
        LocalVariableString questItemTag = questGiver.GetObjectVariable<LocalVariableString>("questitem"+index);
        
        NwItem questItem = playerCharacter.Inventory.Items.First(item => item.Tag == questItemTag);
        
        // END VARIABLE DECLARATION
    
        questGiver.FaceToObject(playerCharacter);
        
        CheckRestrictions(questGiver);

    }
        
    /// <summary>
    /// Checks for class, skill, and prequel quest restrictions when trying to take the quest and makes NPC say the gtfo message
    /// </summary>
    private static void CheckRestrictions(NwCreature questGiver)
    {
        LocalVariableString questRequired  = questGiver.GetObjectVariable<LocalVariableString>("questrequired");
        LocalVariableString speechQuestRequired = questGiver.GetObjectVariable<LocalVariableString>("speechquestrequired");
        LocalVariableString classRequired = questGiver.GetObjectVariable<LocalVariableString>("classrequired");
        LocalVariableString skillRequired = questGiver.GetObjectVariable<LocalVariableString>("skillrequired");
        LocalVariableString rejectionMessage = questGiver.GetObjectVariable<LocalVariableString>("rejectionmessage");
        
    }
}