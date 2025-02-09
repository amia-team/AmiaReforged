using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

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
    /// Checks for class, skill, alignment, and prequel quest requirements when trying to take the quest
    /// and makes NPC say the rejection message
    /// </summary>
    /// <returns>true if requirements are met; false if not</returns>
    private static bool CheckQuestRequirements(NwCreature questGiver, NwCreature playerCharacter)
    {
        // Indexing for the quest variables starts at 1 for legacy reasons;
        // non-indexed quest variables like "questname" are treated as "questname1"
        int index = 1;
        
        LocalVariableString questRequired  = questGiver.GetObjectVariable<LocalVariableString>("questrequired"+index);
        LocalVariableString classRequired = questGiver.GetObjectVariable<LocalVariableString>("classrequired"+index);
        LocalVariableInt classLevelRequired = questGiver.GetObjectVariable<LocalVariableInt>("classlevelrequired"+index);
        LocalVariableString skillRequired = questGiver.GetObjectVariable<LocalVariableString>("skillrequired"+index);
        LocalVariableInt skillRankRequired = questGiver.GetObjectVariable<LocalVariableInt>("skillrankrequired"+index);
        LocalVariableString alignmentRequired = questGiver.GetObjectVariable<LocalVariableString>("alignmentrequired"+index);
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
        if (questRequired.HasNothing && classRequired.HasNothing && skillRequired.HasNothing
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

        if (classRequired.HasValue)
        {
            NwClass? requiredClass = classRequired.Value switch
            {
                "barb" or "barbarian" or "Barb" or "Barbarian" 
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_BARBARIAN),
                "bard" or "Bard" => NwClass.FromClassId(NWScript.CLASS_TYPE_BARD),
                "cleric" or "Cleric" => NwClass.FromClassId(NWScript.CLASS_TYPE_CLERIC),
                "druid" or "Druid" => NwClass.FromClassId(NWScript.CLASS_TYPE_DRUID),
                "fighter" or "Fighter" => NwClass.FromClassId(NWScript.CLASS_TYPE_FIGHTER),
                "monk" or "Monk" => NwClass.FromClassId(NWScript.CLASS_TYPE_MONK),
                "pala" or "paladin" or "Pala" or "Paladin"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_PALADIN),
                "ranger" or "Ranger" => NwClass.FromClassId(NWScript.CLASS_TYPE_RANGER),
                "rogue" or "rouge" or "Rogue" or "Rouge" 
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_ROGUE),
                "sorc" or "sorcerer" or "sorceror" or "Sorc" or "Sorcerer" or "Sorceror"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_SORCERER),
                "wiz" or "wizard" or "Wiz" or "Wizard" 
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_WIZARD),
                "shifter" or "Shifter" => NwClass.FromClassId(NWScript.CLASS_TYPE_SHIFTER),
                "warlock" or "Warlock" => NwClass.FromClassId(Warlock),
                "sd" or "shadowdancer" or "shadow dancer" or "SD" or "Shadowdancer" or "Shadow Dancer"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_SHADOWDANCER),
                "ms" or "masterscout" or "master scout" or "MS" or "Masterscout" or "Master Scout" 
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_HARPER),
                "aa" or "arcane archer" or "AA" or "Arcane Archer"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_ARCANE_ARCHER),
                "ass" or "assassin" or "Ass" or "Assassin"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_ASSASSIN),
                "bg" or "blackguard" or "BG" or "Blackguard" 
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_BLACKGUARD),
                "dc" or "cot" or "divine champion" or "DC" or "CoT" or "Divine Champion"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_DIVINE_CHAMPION),
                "wm" or "weapon master" or "WM" or "Weapon Master" => NwClass.FromClassId(NWScript.CLASS_TYPE_WEAPON_MASTER),
                "pm" or "palemaster" or "pale master" or "PM" or "Palemaster" or "Pale Master"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_PALE_MASTER),
                "dwd" or "dwarven defender" or "DwD" or "Dwarven Defender"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_DWARVEN_DEFENDER),
                "dd" or "rdd" or "dragon disciple" or "DD" or "RDD" or "Dragon Disciple"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_DRAGON_DISCIPLE),
                "kc" or "pdk" or "knight commander" or "KC" or "PDK" or "Knight Commander"
                    => NwClass.FromClassId(NWScript.CLASS_TYPE_PURPLE_DRAGON_KNIGHT),
                "warslinger" or "Warslinger" => NwClass.FromClassId(Warslinger),
                "cavalry" or "cavalier" or "Cavalry" or "Cavalier" => NwClass.FromClassId(Cavalry),
                "twf" or "two weapon fighter" or "two-weapon fighter" or "TWF" or "Two Weapon Fighter" or "Two-Weapon Fighter"
                    => NwClass.FromClassId(TwoWeaponFighter),
                "bowmaster" or "bow master" or "Bowmaster" or "Bow Master" => NwClass.FromClassId(BowMaster),
                "def" or "defender" or "Def" or "Defender" => NwClass.FromClassId(Defender),
                "combat medic" or "Combat Medic" => NwClass.FromClassId(CombatMedic),
                "arbalest" or "Arbalest" => NwClass.FromClassId(Arbalest),
                "duelist" or "duellist" or "Duelist" or "Duellist" => NwClass.FromClassId(Duelist),
                "lycan" or "lycanthrope" or "Lycan" or "Lycanthrope" => NwClass.FromClassId(Lycanthrope),
                "peer" or "peerage" or "Peer" or "Peerage" => NwClass.FromClassId(Peerage),
                "fiendish corrupted" or "Fiendish Corrupted" => NwClass.FromClassId(FiendishCorrupted),
                "bloodsworn" or "Bloodsworn" => NwClass.FromClassId(Bloodsworn),
                _ => null
            };
            if (requiredClass is null) playerCharacter.ControllingPlayer!.SendServerMessage
                ($"DEBUG: \"{classRequired.Value}\" is a wrong input for quest's required class.");

            if (playerCharacter.GetClassInfo(requiredClass).Level < classLevelRequired.Value)
            {
                if (rejectionMessage.HasNothing) 
                    playerCharacter.LoginPlayer!.SendServerMessage("DEBUG: No quest rejection message.");
                else
                    questGiver.SpeakString(rejectionMessage!);
            }
            
            
        }

        return true;
    }
}