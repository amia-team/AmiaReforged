using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.Dynamic.GenericQuest;

public static class QuestRequirements
{
    public static bool CheckQuestRequirements(NwCreature questGiver, NwCreature playerCharacter)
    {
        if (CheckRequiredQuests(questGiver, playerCharacter) is false)
            return false;

        if (CheckRequiredClasses(questGiver, playerCharacter) is false)
            return false;

        if (CheckRequiredAlignments(questGiver, playerCharacter) is false)
            return false;

        if (CheckRequiredSkills(questGiver, playerCharacter) is false)
            return false;

        // set as eg "[message]"
        LocalVariableString rejectionMessage = questGiver.GetObjectVariable<LocalVariableString>("rejection message");

        return false;
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
        // set as eg "fighter 5 |  barbarian 10" or "fighter 5 & barbarian 10"
        LocalVariableString requiredClasses = questGiver.GetObjectVariable<LocalVariableString>("required classes");
        
        return false;
    }

    /// <summary>
    /// Checks that the quests required to pick up the new quest are completed
    /// </summary>
    /// <returns>True if required quests are completed, false if not</returns>
    private static bool CheckRequiredQuests(NwCreature questGiver, NwCreature playerCharacter)
    {
        // set in the toolset as eg "[quest name 1] | [quest name 2]" or "[quest name 1] & [quest name 2]"
        LocalVariableString requiredQuests = questGiver.GetObjectVariable<LocalVariableString>("required quests");

        // If no requirements are set, return true so the quest script knows that the quest can be taken
        if (requiredQuests.HasNothing)
            return true;


        // If there is a quest requirement, check that the required quest has been completed to return true and allow
        // character to take on the quest; otherwise play rejection message, inform the player of the quest they need
        // to complete, and return false in the quest script to not start the quest
        if (requiredQuests.HasValue)
        {
            string[] requiredQuestsAny = requiredQuests.Value!.Split(" | ");
            string[] requiredQuestsAll = requiredQuests.Value!.Split(" & ");

            NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");

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
        }

        return false;
    }
}