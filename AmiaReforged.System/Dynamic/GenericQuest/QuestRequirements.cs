using System.Text.RegularExpressions;
using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
using Anvil.API;
using static System.Int32;

namespace AmiaReforged.System.Dynamic.GenericQuest;

public static class QuestRequirements
{
    public static string? CheckQuestRequirements(NwCreature questGiver, NwCreature playerCharacter)
    {
        if (CheckRequiredQuests(questGiver, playerCharacter) is false)
        {
            
        }

        if (CheckRequiredClasses(questGiver, playerCharacter) is false)
        {
                
        }

        if (CheckRequiredAlignments(questGiver, playerCharacter) is false)
        {
                
        }
            
        if (CheckRequiredSkills(questGiver, playerCharacter) is false)
        {
                
        }

        // set as eg "[message]"
        LocalVariableString rejectionMessage = questGiver.GetObjectVariable<LocalVariableString>("rejection message");

        return rejectionMessage;
    }

    private static bool CheckRequiredSkills(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set as eg "perform 5 |  persuade 10" or "perform 5 & persuade 10"
        LocalVariableString requiredSkills = questGiver.GetObjectVariable<LocalVariableString>("required skills");

        return false;
    }

    private static bool CheckRequiredAlignments(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set as eg "neutral | evil" or "neutral & evil"
        LocalVariableString requiredAlignments =
            questGiver.GetObjectVariable<LocalVariableString>("required alignments");

        return false;
    }

    private static bool CheckRequiredClasses(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set as eg "fighter 5 | barbarian 10" or "fighter 5 & barbarian 10"
        LocalVariableString requiredClasses = questGiver.GetObjectVariable<LocalVariableString>("required classes");
        
        // If no requirements are set, return true
        if (requiredClasses.HasNothing)
            return true;
        
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");
        
        
        string[] requiredClassesAny = requiredClasses.Value!.Split(" | ");
        string[] requiredClassesAll = requiredClasses.Value!.Split(" & ");
        string[] requiredClassesNone = requiredClasses.Value!.Split('!');
        
        // Populate an int array with values from the requiredClasses string;
        // replace null values with 1s in case quest maker hasn't specified the class level
        int?[] requiredClassesAnyLevels = new int?[requiredClassesAny.Length];
        for (int i = 0; i < requiredClassesAny.Length; i++)
            requiredClassesAnyLevels[i] = 
                (TryParse(requiredClassesAll[i], out int level) ? level : (int?)null) ?? 1;
        
        int?[] requiredClassesAllLevels = new int?[requiredClassesAll.Length];
        for (int i = 0; i < requiredClassesAll.Length; i++)
            requiredClassesAllLevels[i] = 
                (TryParse(requiredClassesAll[i], out int level) ? level : (int?)null) ?? 1;
        
        

        /*requiredClassesAny.Take()
        foreach (string requiredClass in requiredClassesAny)
        {
            NwClass? requiredClass = requiredClasses.Value switch
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
            "wm" or "weapon master" or "WM" or "Weapon Master" => NwClass.FromClassId(NWScript
                .CLASS_TYPE_WEAPON_MASTER),
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
            "twf" or "two weapon fighter" or "two-weapon fighter" or "TWF" or "Two Weapon Fighter"
                or "Two-Weapon Fighter"
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
        }
        */
        
        
        if (requiredClasses is null)
            playerCharacter.ControllingPlayer!.SendServerMessage
                ($"DEBUG: \"{requiredClasses.Value}\" is a wrong input for quest's required class.");

        
        return false;
    }

    /// <summary>
    /// Checks that the quests required to pick up the new quest are completed
    /// </summary>
    /// <returns>True if required quests are completed, false if not</returns>
    private static bool CheckRequiredQuests(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set in the toolset as eg "[quest name 1] | [quest name 2]" or "[quest name 1] & [quest name 2]"
        string? requiredQuests = questGiver.GetObjectVariable<LocalVariableString>("required quests").Value;

        // If no requirements are set, return true
        if (requiredQuests is null)
            return true;
        
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");
        

        // If there is a quest requirement, check that the required quest has been completed to return true and allow
        // character to take on the quest; otherwise play rejection message, inform the player of the quest they need
        // to complete, and return false in the quest script to not start the quest
        
        string[] requiredQuestsAny = QuestUtilFuncs.SanitizeAndSplit(requiredQuests, "||");
        string[] requiredQuestsAll = QuestUtilFuncs.SanitizeAndSplit(requiredQuests, "&&");

        // If there's only one required quest, check for it first
        if (requiredQuestsAny.Length == 1)
        {
            int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(requiredQuestsAny[0]).Value;
            if (requiredQuestStatus is QuestCompleted)
                return true;

            string requiredQuest = requiredQuestsAny[0].ColorString(ColorConstants.Green);

            playerCharacter.LoginPlayer!.SendServerMessage
                ($"You must complete {requiredQuest} before you may begin this one.");

            return false;
        }

        // If required quests are seperated by " | ", character must have completed one of the required quests
        if (requiredQuestsAny.Length > 1)
            for (int i = 0; i < requiredQuestsAny.Length; i++)
            {
                int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(requiredQuestsAny[i]).Value;

                if (requiredQuestStatus is QuestCompleted) return true;

                if (i != requiredQuestsAny.Length - 1) continue;

                // Last loop iteration if there's still no qualifying quest completed

                string requiredQuestsJoined =
                    string.Join(", ", requiredQuestsAny).ColorString(ColorConstants.Green);
                playerCharacter.LoginPlayer!.SendServerMessage
                    ($"You must complete one of the following quests before you may begin this one: {requiredQuestsJoined}");

                return false;
            }

        // If required quests are seperated by " & ", character must have completed all the required quests
        List<string> requiredQuestsList = null!;

        foreach (string requiredQuest in requiredQuestsAll)
        {
            int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(requiredQuest).Value;
            if (requiredQuestStatus is not QuestCompleted)
                requiredQuestsList.Add(requiredQuest);
        }

        switch (requiredQuestsList.Count)
        {
            case 0:
                return true;
            case < 0:
            {
                string requiredQuestsJoined =
                    string.Join(", ", requiredQuestsList).ColorString(ColorConstants.Green);
                playerCharacter.LoginPlayer!.SendServerMessage
                    ($"You must complete one of the following quests before you may begin this one: {requiredQuestsJoined}");

                return false;
            }
        }

        return false;
    }
}