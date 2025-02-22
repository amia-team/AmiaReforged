using System.Text.RegularExpressions;
using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;

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
        {
            questVarSplit[i] = questVarSplit[i].TrimStart();
            questVarSplit[i] = questVarSplit[i].TrimEnd();
        }
        
        return questVarSplit;
    }
    
    /// <summary>
    /// Sends a uniform debug message to the player for incorrect inputs to quest giver's local variables
    /// </summary>
    public static void SendQuestDebug(NwPlayer player, string varName, string varElement)
    {
        player.SendServerMessage($"DEBUG: Input \"{varElement}\" in quest NPC's local var {varName} is invalid.");
    }
}