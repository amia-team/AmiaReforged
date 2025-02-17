using System.Text.RegularExpressions;

namespace AmiaReforged.System.Dynamic.GenericQuest;

public class QuestUtilFuncs
{
    /// <summary>
    /// Sanitizes dev made inputs to quest variables by removing unnecessary whitespace.
    /// </summary>
    /// <returns>Returns the quest variable string split</returns>
    public static string[] SanitizeAndSplit(string questVariable, string separator)
    {
        questVariable = Regex.Replace(questVariable.Trim(), @"\s+", " ");
        
        string[] questVariableSplit = questVariable.Split(separator);

        for (int i = 0; i < questVariableSplit.Length; i++)
        {
            questVariableSplit[i] = questVariableSplit[i].TrimStart();
            questVariableSplit[i] = questVariableSplit[i].TrimEnd();
        }
        
        return questVariableSplit;
    }
}