using System.Text.RegularExpressions;
using Anvil.API;

namespace AmiaReforged.System.Dynamic.GenericQuest;

public static class QuestUtil
{
    /// <summary>
    /// Sanitizes dev made inputs to quest variables by removing unnecessary whitespace.
    /// </summary>
    /// <returns>Returns the quest variable string split</returns>
    public static string[] SanitizeAndSplit(string questVar, string separator)
    {
        questVar = Regex.Replace(questVar.Trim(), @"\s+", " ");
        
        string[] questVarSplit = questVar.Split(separator);

        for (int i = 0; i < questVarSplit.Length; i++) 
            questVarSplit[i] = questVarSplit[i].Trim();
        
        return questVarSplit;
    }
    
    /// <summary>
    /// Sends a uniform debug message to the player for incorrect inputs to quest giver's local variables
    /// </summary>
    public static void SendQuestDebug(NwPlayer player, string varName, string varElement)
    {
        player.SendServerMessage($"DEBUG: Input \"{varElement}\" in quest NPC's local var {varName} is invalid.");
    }
    
    /// <summary>
    /// Validates different variable names so it's less finicky about the spelling
    /// </summary>
    /// <param name="questCreature">The creature whose local variables you're using</param> 
    /// <param name="varName">The "real" variable name you need to validate for different inputs</param> 
    /// <returns>The right local variable to carry out quest actions; null if no valid variable is found</returns>
    public static ObjectVariable? ValidateVarName(NwCreature questCreature, string varName)
    {
        string[] possibleVarNames = 
        { 
            varName, // eg "required quests"
            string.Concat(varName.Where(c => !char.IsWhiteSpace(c))), // "requiredquests"
            UppercaseFirst(varName), // "Required quests"
            UppercaseFirst(varName.Split(' ')[0]) + ' ' + UppercaseFirst(varName.Split(' ')[1]), // "Required Quests"
            UppercaseFirst(varName.Split(' ')[0]) + varName.Split(' ')[1], // "Requiredquests"
            UppercaseFirst(varName.Split(' ')[0]) + UppercaseFirst(varName.Split(' ')[1]), // "RequiredQuests"
        };

        ObjectVariable? validatedVar = null;
        
        foreach (string possibleVarName in possibleVarNames)
            validatedVar = questCreature.LocalVariables.First(var => var.Name == possibleVarName);

        return validatedVar;
        
        string UppercaseFirst(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            
            return char.ToUpper(str[0]) + str[1..].ToLower();
        }
    }
}