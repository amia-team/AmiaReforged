using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Dynamic.GenericQuest;

/// <summary>
/// This code injects new and improved behaviour and setup for the old ee_generic_quest script.
/// The old ee_generic_quest script is kept unchanged, so people preferring that method can use it.
/// </summary>
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
        
        // START VARIABLE DECLARATION FOR LEGACY VARIABLES

        NwCreature playerCharacter = eventData.PlayerSpeaker!.ControlledCreature!;
        NwCreature questGiver = eventData.Creature;
        
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");
        
        LocalVariableString legacyQuestName = questGiver.GetObjectVariable<LocalVariableString>("questname");
        LocalVariableString questNames = questGiver.GetObjectVariable<LocalVariableString>("quest names");
        if (legacyQuestName.HasNothing && questNames.HasNothing)
            playerCharacter.LoginPlayer!.SendServerMessage
                ("DEBUG: No quest names found in quest NPC's local variables.");
        
        LocalVariableString legacySpeechWithQuest = questGiver.GetObjectVariable<LocalVariableString>("speechwithquest");
        LocalVariableString messagesWithQuest = questGiver.GetObjectVariable<LocalVariableString>("messages with quest taken");
        if (legacySpeechWithQuest.HasNothing && messagesWithQuest.HasNothing) 
            playerCharacter.LoginPlayer!.SendServerMessage
                ("DEBUG: No messages with quest taken found in quest NPC's local variables.");
        
        LocalVariableString legacySpeechQuestDone = questGiver.GetObjectVariable<LocalVariableString>("speechquestdone");
        LocalVariableString messagesWithQuestDone = questGiver.GetObjectVariable<LocalVariableString>("messages with quest completed");
        if (legacySpeechQuestDone.HasNothing && messagesWithQuestDone.HasNothing)
            playerCharacter.LoginPlayer!.SendServerMessage
                ("DEBUG: No messages with quest completed found in quest NPC's local variables.");
        
        
        LocalVariableString questItemTag = questGiver.GetObjectVariable<LocalVariableString>("questitem");
        LocalVariableString questItemTags = questGiver.GetObjectVariable<LocalVariableString>("quest item tags");
        if (questItemTag.HasNothing && questItemTags.HasNothing)
            playerCharacter.LoginPlayer!.SendServerMessage
                ("DEBUG: No quest item tags found in quest NPC's local variables.");
        
        NwItem questItem = playerCharacter.Inventory.Items.First(item => item.Tag == questItemTag);
        
        // END VARIABLE DECLARATION
        questGiver.FaceToObject(playerCharacter);
        
        QuestRequirements.CheckQuestRequirements(questGiver, playerCharacter);

    }
        

}