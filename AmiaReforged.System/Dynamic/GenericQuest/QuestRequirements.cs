﻿using System.Text.RegularExpressions;
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
        // set as eg "perform 5 ||  persuade 10" or "perform 5 && persuade 10"
        LocalVariableString requiredSkills = questGiver.GetObjectVariable<LocalVariableString>("required skills");

        return false;
    }

    private static bool CheckRequiredAlignments(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set as eg "neutral || evil" or "neutral && evil"
        LocalVariableString requiredAlignments =
            questGiver.GetObjectVariable<LocalVariableString>("required alignments");

        return false;
    }

    private static bool CheckRequiredClasses(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set as eg "fighter 5 || barbarian 10" or "fighter 5 && barbarian 10"
        string? requiredClasses = questGiver.GetObjectVariable<LocalVariableString>("required classes").Value;
        
        // If no requirements are set, return true
        if (requiredClasses is null)
            return true;

        string[] requiredClassesAny = QuestUtil.SanitizeAndSplit(requiredClasses, "||");
        string[] requiredClassesAll = QuestUtil.SanitizeAndSplit(requiredClasses, "&&");

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
                NwClass? requiredClass = QuestUtil.GetRequiredClass(requiredClassesAny[i]);
                int? requiredLevel = requiredClassesAnyLevels[i];
                
                if (requiredClass is null) playerCharacter.LoginPlayer!.SendServerMessage
                    ($"DEBUG: Required class \"{requiredClassesAny[i]}\" is an invalid input.");
                
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
            NwClass? requiredClass = QuestUtil.GetRequiredClass(requiredClassesAll[i]);
            int? requiredLevel = requiredClassesAllLevels[i];
            
            if (requiredClass is null)
                playerCharacter.LoginPlayer!.SendServerMessage
                    ($"DEBUG: Required class \"{requiredClassesAll[i]}\" is an invalid input.");
            
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
        
        string[] requiredQuestsAny = QuestUtil.SanitizeAndSplit(requiredQuests, "||");
        string[] requiredQuestsAll = QuestUtil.SanitizeAndSplit(requiredQuests, "&&");

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