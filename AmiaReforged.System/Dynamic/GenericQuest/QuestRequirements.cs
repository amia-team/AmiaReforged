using System.Text.RegularExpressions;
using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;
using static System.Int32;

namespace AmiaReforged.System.Dynamic.GenericQuest;

public static class QuestRequirements
{
    public static string? CheckQuestRequirements(NwCreature questGiver, NwCreature playerCharacter)
    {
        LocalVariableString requiredQuests = questGiver.GetObjectVariable<LocalVariableString>("required quests");
        
        if (requiredQuests.HasValue && CheckRequiredQuests(playerCharacter, requiredQuests) is false)
        {
            
        }
        
        LocalVariableString requiredClasses = questGiver.GetObjectVariable<LocalVariableString>("required classes");
        
        if (requiredClasses.HasValue && CheckRequiredClasses(playerCharacter, requiredClasses) is false)
        {
                
        }
        
        LocalVariableString requiredAlignments = questGiver.GetObjectVariable<LocalVariableString>("required alignments");
        
        if (requiredAlignments.HasValue && CheckRequiredAlignments(playerCharacter, requiredAlignments) is false)
        {
                
        }
        
        LocalVariableString requiredSkills = questGiver.GetObjectVariable<LocalVariableString>("required skills");
        
        if (requiredSkills.HasValue && CheckRequiredSkills(playerCharacter, requiredSkills) is false)
        {
                
        }

        // set as eg "[message]"
        LocalVariableString rejectionMessage = questGiver.GetObjectVariable<LocalVariableString>("rejection message");

        return rejectionMessage;
    }

    private static bool CheckRequiredSkills(NwCreature playerCharacter, LocalVariableString requiredSkills)
    {
        string[] requiredSkillsAny = QuestUtil.SanitizeAndSplit(requiredSkills.Value!, "||");
        string[] requiredSkillsAll = QuestUtil.SanitizeAndSplit(requiredSkills.Value!, "&&");

        string requiredSkillsJoined;
        
        if (requiredSkillsAny.Length >= requiredSkillsAll.Length)
        {
            // Populate an int array with skill levels from the required skilles string;
            // replace null values with 1s in case quest maker hasn't specified the skill level
            
            int?[] requiredSkillsAnyRank = new int?[requiredSkillsAny.Length];
            for (int i = 0; i < requiredSkillsAny.Length; i++)
            {
                requiredSkillsAny[i] = Regex.Match(requiredSkillsAny[i], @"\d+").Value;
                requiredSkillsAnyRank[i] = 
                    TryParse(requiredSkillsAny[i], out int level) ? level : (int?)null ?? 1;
            }
            
            for (int i = 0; i < requiredSkillsAny.Length; i++)
            {
                NwSkill? requiredSkill = GetRequiredSkill(requiredSkillsAny[i]);

                int? requiredRank = requiredSkillsAnyRank[i];
                
                if (requiredSkill is null) 
                    QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredSkills.Name, requiredSkillsAny[i]);
                
                // If the skill requirement has '!' in it, then that skill is banned from taking the quest
                if (requiredSkillsAny[i].Contains('!') 
                    && playerCharacter.GetSkillRank(requiredSkill) >= requiredRank)
                {
                    playerCharacter.LoginPlayer!.SendServerMessage
                        ($"Skill {requiredSkillsAny[i]} cannot take this quest.");
                    return false;
                }

                if (playerCharacter.GetSkillRank(requiredSkill) >= requiredRank)
                    return true;
            }
            
            requiredSkillsJoined = string.Join("; ", requiredSkillsAny);
            
            playerCharacter.LoginPlayer!.SendServerMessage
                ($"One of the following skilles is required to take this quest: {requiredSkillsJoined}");
            return false;
        }
        
        int?[] requiredSkillsAllRank = new int?[requiredSkillsAll.Length];
        for (int i = 0; i < requiredSkillsAll.Length; i++)
        {
            requiredSkillsAll[i] = Regex.Match(requiredSkillsAll[i], @"\d+").Value;
            requiredSkillsAllRank[i] = 
                TryParse(requiredSkillsAll[i], out int level) ? level : (int?)null ?? 1;
        }
        
        List<string> requiredSkillsList = null!;
        
        for (int i = 0; i < requiredSkillsAll.Length; i++)
        {
            NwSkill? requiredSkill = GetRequiredSkill(requiredSkillsAll[i]);
            int? requiredRank = requiredSkillsAllRank[i];
            
            if (requiredSkill is null) 
                QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredSkills.Name, requiredSkillsAll[i]);

            // Add required skills that the PC doesn't have to the naughty list
            if (playerCharacter.GetSkillRank(requiredSkill) < requiredRank)
                requiredSkillsList.Add(requiredSkillsAll[i]);
        }
        
        // If nothing was added to the naughty list, it means that PC has all required skills and quest can be taken 
        if (requiredSkillsList.IsNullOrEmpty()) return true;
        
        requiredSkillsJoined = string.Join("; ", requiredSkillsList).ColorString(ColorConstants.Green);
        
        playerCharacter.LoginPlayer!.SendServerMessage
            ($"The following skills are required to take this quest: {requiredSkillsJoined}");
        
        return false;
        
        NwSkill? GetRequiredSkill(string p0)
        {
            throw new NotImplementedException();
        }
    }

    private static bool CheckRequiredAlignments(NwCreature playerCharacter, LocalVariableString requiredAlignments)
    {
        string[] requiredAlignmentsAny = QuestUtil.SanitizeAndSplit(requiredAlignments.Value!, "||");

        foreach (string requiredAlignmentString in requiredAlignmentsAny)
        {
            Alignment? requiredAlignment = GetRequiredAlignment(requiredAlignmentString);

            if (requiredAlignment is null)
            {
                QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredAlignments.Name, requiredAlignmentString);
                continue;
            }
            
            // check if there are two words in the variable like "neutral evil" instead of just "evil"
            if (requiredAlignmentString.Contains(' '))
            {
                Alignment? requiredAlignmentLawChaos =
                    GetRequiredAlignment(requiredAlignmentString.Split(' ')[0]);
                Alignment? requiredAlignmentGoodEvil = 
                    GetRequiredAlignment(requiredAlignmentString.Split(' ')[1]);
                
                if (requiredAlignmentLawChaos == playerCharacter.LawChaosAlignment 
                    && requiredAlignmentGoodEvil == playerCharacter.GoodEvilAlignment)
                    return true;

                continue;
            }
            
            if (requiredAlignment == playerCharacter.GoodEvilAlignment ||
                requiredAlignment == playerCharacter.LawChaosAlignment)
                return true;
        }

        string requiredAlignmentsJoined = string.Join(", ", requiredAlignmentsAny);
        
        playerCharacter.LoginPlayer!.SendServerMessage
            ($"This quest requires any of the following alignments:{requiredAlignmentsJoined}");

        return false;
        
        // Local helper function to keep code more readable and shorter
        Alignment? GetRequiredAlignment(string alignmentRequirementVar)
        {
            return alignmentRequirementVar switch
            {
                not null when alignmentRequirementVar.Contains("go", StringComparison.CurrentCultureIgnoreCase) 
                    => Alignment.Good,
                not null when alignmentRequirementVar.Contains("ev", StringComparison.CurrentCultureIgnoreCase) 
                    => Alignment.Evil,
                not null when alignmentRequirementVar.Contains("ne", StringComparison.CurrentCultureIgnoreCase) 
                    => Alignment.Neutral,
                not null when alignmentRequirementVar.Contains("la", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Lawful,
                not null when alignmentRequirementVar.Contains("tr", StringComparison.CurrentCultureIgnoreCase) 
                    => Alignment.Neutral,
                not null when alignmentRequirementVar.Contains("ch", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Chaotic,
                _ => null
            };
        }
    }

    private static bool CheckRequiredClasses(NwCreature playerCharacter, LocalVariableString requiredClasses)
    {
        string[] requiredClassesAny = QuestUtil.SanitizeAndSplit(requiredClasses.Value!, "||");
        string[] requiredClassesAll = QuestUtil.SanitizeAndSplit(requiredClasses.Value!, "&&");

        string requiredClassesJoined;
        
        if (requiredClassesAny.Length >= requiredClassesAll.Length)
        {
            // Populate an int array with class levels from the required classes string;
            // replace null values with 1s in case quest maker hasn't specified the class level
            
            int?[] requiredClassesAnyLevels = new int?[requiredClassesAny.Length];
            for (int i = 0; i < requiredClassesAny.Length; i++)
            {
                requiredClassesAny[i] = Regex.Match(requiredClassesAny[i], @"\d+").Value;
                requiredClassesAnyLevels[i] = 
                    TryParse(requiredClassesAny[i], out int level) ? level : (int?)null ?? 1;
            }
            
            for (int i = 0; i < requiredClassesAny.Length; i++)
            {
                NwClass? requiredClass = GetRequiredClass(requiredClassesAny[i]);
                int? requiredLevel = requiredClassesAnyLevels[i];
                
                if (requiredClass is null) 
                    QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredClasses.Name, requiredClassesAny[i]);
                
                // If the class requirement has '!' in it, then that class is banned from taking the quest
                if (requiredClassesAny[i].Contains('!') 
                    && playerCharacter.GetClassInfo(requiredClass)!.Level >= requiredLevel)
                {
                    playerCharacter.LoginPlayer!.SendServerMessage
                        ($"Class {requiredClassesAny[i]} cannot take this quest.");
                    return false;
                }

                if (playerCharacter.GetClassInfo(requiredClass)!.Level >= requiredLevel)
                    return true;
            }
            
            requiredClassesJoined = string.Join("; ", requiredClassesAny);
            
            playerCharacter.LoginPlayer!.SendServerMessage
                ($"One of the following classes is required to take this quest: {requiredClassesJoined}");
            return false;
        }
        
        int?[] requiredClassesAllLevels = new int?[requiredClassesAll.Length];
        for (int i = 0; i < requiredClassesAll.Length; i++)
        {
            requiredClassesAll[i] = Regex.Match(requiredClassesAll[i], @"\d+").Value;
            requiredClassesAllLevels[i] = 
                TryParse(requiredClassesAll[i], out int level) ? level : (int?)null ?? 1;
        }
        
        List<string> requiredClassesList = null!;
        
        for (int i = 0; i < requiredClassesAll.Length; i++)
        {
            NwClass? requiredClass = GetRequiredClass(requiredClassesAll[i]);
            int? requiredLevel = requiredClassesAllLevels[i];
            
            if (requiredClass is null) 
                QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredClasses.Name, requiredClassesAll[i]);

            // Add required classes that the PC doesn't have to the naughty list
            if (playerCharacter.GetClassInfo(requiredClass)!.Level < requiredLevel)
                requiredClassesList.Add(requiredClassesAll[i]);
        }
        
        // If nothing was added to the naughty list, it means that PC has all required classes and quest can be taken 
        if (requiredClassesList.IsNullOrEmpty()) return true;
        
        requiredClassesJoined = string.Join("; ", requiredClassesList).ColorString(ColorConstants.Green);
        
        playerCharacter.LoginPlayer!.SendServerMessage
            ($"The following classes are required to take this quest: {requiredClassesJoined}");
        
        return false;
        
        // Local helper function to keep the actual code more readable and shorter
        NwClass? GetRequiredClass(string classRequirementVar)
        {
            int classId = classRequirementVar switch
            {
                not null when classRequirementVar.Contains("barb", StringComparison.CurrentCultureIgnoreCase) 
                    => NWScript.CLASS_TYPE_BARBARIAN,
                not null when classRequirementVar.Contains("bard", StringComparison.CurrentCultureIgnoreCase) 
                    => NWScript.CLASS_TYPE_BARD,
                not null when classRequirementVar.Contains("cler", StringComparison.CurrentCultureIgnoreCase) 
                    => NWScript.CLASS_TYPE_CLERIC,
                not null when classRequirementVar.Contains("drui", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DRUID,
                not null when classRequirementVar.Contains("fight", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_FIGHTER,
                not null when classRequirementVar.Contains("monk", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_MONK,
                not null when classRequirementVar.Contains("pala", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_PALADIN,
                not null when classRequirementVar.Contains("rang", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_RANGER,
                not null when classRequirementVar.Contains("rog", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_ROGUE,
                not null when classRequirementVar.Contains("sorc", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_SORCERER,
                not null when classRequirementVar.Contains("wiz", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_WIZARD,
                not null when classRequirementVar.Contains("shif", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_SHIFTER,
                not null when classRequirementVar.Contains("warl", StringComparison.CurrentCultureIgnoreCase)
                    => Warlock,
                not null when classRequirementVar.Contains("ass", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_ASSASSIN,
                not null when classRequirementVar.Contains("arca", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_ARCANE_ARCHER,
                not null when classRequirementVar.Contains("blac", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_BLACKGUARD,
                not null when classRequirementVar.Contains("div", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DIVINE_CHAMPION,
                not null when classRequirementVar.Contains("dra", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DRAGON_DISCIPLE,
                not null when classRequirementVar.Contains("dwar", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DWARVEN_DEFENDER,
                not null when classRequirementVar.Contains("knig", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_PURPLE_DRAGON_KNIGHT,
                not null when classRequirementVar.Contains("", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_BLACKGUARD,
                not null when classRequirementVar.Contains("pale", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_PALE_MASTER,
                not null when classRequirementVar.Contains("shado", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_SHADOWDANCER,
                not null when classRequirementVar.Contains("weap", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_WEAPON_MASTER,
                not null when classRequirementVar.Contains("scou", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_HARPER,
                not null when classRequirementVar.Contains("bow", StringComparison.CurrentCultureIgnoreCase)
                    => BowMaster,
                not null when classRequirementVar.Contains("cav", StringComparison.CurrentCultureIgnoreCase)
                    => Cavalry,
                not null when classRequirementVar.Contains("def", StringComparison.CurrentCultureIgnoreCase)
                    => Defender,
                not null when classRequirementVar.Contains("esc", StringComparison.CurrentCultureIgnoreCase)
                    => EscapeArtist,
                not null when classRequirementVar.Contains("two", StringComparison.CurrentCultureIgnoreCase)
                    => TwoWeaponFighter,
                not null when classRequirementVar.Contains("medi", StringComparison.CurrentCultureIgnoreCase)
                    => CombatMedic,
                not null when classRequirementVar.Contains("arbal", StringComparison.CurrentCultureIgnoreCase)
                    => Arbalest,
                not null when classRequirementVar.Contains("duel", StringComparison.CurrentCultureIgnoreCase)
                    => Duelist,
                not null when classRequirementVar.Contains("lycan", StringComparison.CurrentCultureIgnoreCase)
                    => Lycanthrope,
                not null when classRequirementVar.Contains("peer", StringComparison.CurrentCultureIgnoreCase)
                    => Peerage,
                not null when classRequirementVar.Contains("corr", StringComparison.CurrentCultureIgnoreCase)
                    => FiendishCorrupted,
                not null when classRequirementVar.Contains("bloo", StringComparison.CurrentCultureIgnoreCase)
                    => Bloodsworn,
                _ => -1
            };
    
            return NwClass.FromClassId(classId);
        }
    }

    /// <summary>
    /// Checks that the quests required to pick up the new quest are completed
    /// </summary>
    /// <returns>True if required quests are completed, false if not</returns>
    private static bool CheckRequiredQuests(NwCreature playerCharacter, LocalVariableString requiredQuests)
    {
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");
        
        // If there is a quest requirement, check that the required quest has been completed to return true and allow
        // character to take on the quest; otherwise play rejection message, inform the player of the quest they need
        // to complete, and return false in the quest script to not start the quest
        
        string[] requiredQuestsAny = QuestUtil.SanitizeAndSplit(requiredQuests.Value!, "||");
        string[] requiredQuestsAll = QuestUtil.SanitizeAndSplit(requiredQuests.Value!, "&&");

        // If required quests are seperated by "||", character must have completed one of the required quests
        string requiredQuestsJoined;

        if (requiredQuestsAny.Length >= requiredQuestsAll.Length)
        {
            foreach (string requiredQuest in requiredQuestsAny)
            {
                int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(requiredQuest).Value;

                if (requiredQuestStatus is QuestCompleted) return true;
            }
            
            requiredQuestsJoined = string.Join("; ", requiredQuestsAny).ColorString(ColorConstants.Green);
            
            playerCharacter.LoginPlayer!.SendServerMessage
                ($"You must complete one of the following quests before you may begin this one: {requiredQuestsJoined}");

            return false;
        }
            
        
        // If required quests are seperated by "&&", character must have completed all the required quests
        List<string> requiredQuestsList = null!;

        foreach (string requiredQuest in requiredQuestsAll)
        {
            int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(requiredQuest).Value;
            
            // Add uncompleted required quests to a naughty list
            if (requiredQuestStatus is not QuestCompleted)
                requiredQuestsList.Add(requiredQuest);
        }
        
        // If nothing was added to the naughty list, it means that all required quests are completed and quest can be taken
        if (requiredQuestsList.IsNullOrEmpty()) return true;
        
        requiredQuestsJoined = string.Join("; ", requiredQuestsList).ColorString(ColorConstants.Green);
        
        playerCharacter.LoginPlayer!.SendServerMessage
            ($"You must complete all of the following quests before you may begin this one: {requiredQuestsJoined}");

        return false;
    }
}