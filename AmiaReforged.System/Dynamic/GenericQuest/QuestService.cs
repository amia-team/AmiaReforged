using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
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
        
        // Indexing for the quest variables starts at 1 for legacy reasons;
        // non-indexed quest variables like "questname" are treated as "questname1"
        
        int index = 1;
        
        // START VARIABLE DECLARATION

        NwCreature playerCharacter = eventData.PlayerSpeaker!.ControlledCreature!;
        NwCreature questGiver = eventData.Creature;
        
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");

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
        
        CheckQuestRequirements(questGiver, playerCharacter);

    }
        
    /// <summary>
    /// Checks for class, skill, and prequel quest requirements when trying to take the quest and makes NPC say the gtfo message
    /// </summary>
    /// <returns>true if requirements are met; false if not</returns>
    private static bool CheckQuestRequirements(NwCreature questGiver, NwCreature playerCharacter)
    {
        LocalVariableString questRequired  = questGiver.GetObjectVariable<LocalVariableString>("questrequired");
        LocalVariableString classesRequired = questGiver.GetObjectVariable<LocalVariableString>("classesrequired");
        LocalVariableInt classLevelRequired = questGiver.GetObjectVariable<LocalVariableInt>("classlevelrequired");
        LocalVariableString skillsRequired = questGiver.GetObjectVariable<LocalVariableString>("skillsrequired");
        LocalVariableInt skillRankRequired = questGiver.GetObjectVariable<LocalVariableInt>("skillrankrequired");
        LocalVariableString rejectionMessage = questGiver.GetObjectVariable<LocalVariableString>("rejectionmessage");
        
        // Legacy restriction variables to keep existing quests functional
        LocalVariableString classRestrictedOn = questGiver.GetObjectVariable<LocalVariableString>("classrestrictedon");
        LocalVariableString skillRestrictedOn = questGiver.GetObjectVariable<LocalVariableString>("skillrestrictedon");
        LocalVariableInt classRestricted = questGiver.GetObjectVariable<LocalVariableInt>("classrestricted");
        LocalVariableInt skillRestricted = questGiver.GetObjectVariable<LocalVariableInt>("skillrestricted");
        LocalVariableInt skillRestrictedPoints = questGiver.GetObjectVariable<LocalVariableInt>("skillrestrictedpoints");
        LocalVariableInt skillRestrictedPointsBase = questGiver.GetObjectVariable<LocalVariableInt>("skillrestrictedpointsbase");
        LocalVariableString speechQuestRequired = questGiver.GetObjectVariable<LocalVariableString>("speechquestrequired");
        
        // If no requirements are set, return to quest script
        if (questRequired.HasNothing && classesRequired.HasNothing && skillsRequired.HasNothing
            && classRestrictedOn.HasNothing && skillRestrictedOn.HasNothing) return true;

        if (questRequired.HasValue)
        {
            NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");
            int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(questRequired.Value!);

            if (requiredQuestStatus is not QuestCompleted)
            {
                if (rejectionMessage.HasNothing) 
                    playerCharacter.LoginPlayer!.SendServerMessage("DEBUG: No quest rejection message.");
                else
                    questGiver.SpeakString(rejectionMessage!);

                string requiredQuestGreen = questRequired.Value!.ColorString(ColorConstants.Green);
                playerCharacter.LoginPlayer!.SendServerMessage
                    ($"You must complete {requiredQuestGreen} before you may begin this one.");

                return false;
            }
        }
        

        return true;
    }
}