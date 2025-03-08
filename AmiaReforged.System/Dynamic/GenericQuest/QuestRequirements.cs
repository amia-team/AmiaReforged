using System.Text.RegularExpressions;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;
using static AmiaReforged.System.Dynamic.GenericQuest.QuestConstants;
using static System.Int32;

namespace AmiaReforged.System.Dynamic.GenericQuest;

public static class QuestRequirements
{
    public static string? CheckQuestRequirements(NwCreature questGiver, NwCreature playerCharacter)
    {
        LocalVariableString? requiredQuests =
            QuestUtil.ValidateVarName(questGiver, varName: "required quests") as LocalVariableString;

        if (requiredQuests is not null && CheckRequiredQuests(playerCharacter, requiredQuests) is false)
        {
        }

        LocalVariableString? requiredClasses =
            QuestUtil.ValidateVarName(questGiver, varName: "required classes") as LocalVariableString;

        if (requiredClasses is not null && CheckRequiredClasses(playerCharacter, requiredClasses) is false)
        {
        }

        LocalVariableString? requiredAlignments =
            QuestUtil.ValidateVarName(questGiver, varName: "required alignments") as LocalVariableString;

        if (requiredAlignments is not null && CheckRequiredAlignments(playerCharacter, requiredAlignments) is false)
        {
        }

        LocalVariableString? requiredSkills =
            QuestUtil.ValidateVarName(questGiver, varName: "required skills") as LocalVariableString;

        if (requiredSkills is not null && CheckRequiredSkills(playerCharacter, requiredSkills) is false)
        {
        }

        // set as eg "[message]"
        LocalVariableString rejectionMessage =
            questGiver.GetObjectVariable<LocalVariableString>(name: "rejection message");

        return rejectionMessage;
    }

    private static bool CheckRequiredSkills(NwCreature playerCharacter, LocalVariableString requiredSkills)
    {
        // Required skills are set up in the local var as eg "open lock 10 || tumble 5" or "open lock 10 && tumble 5"
        string[] requiredSkillsAny = QuestUtil.SanitizeAndSplit(requiredSkills.Value!, separator: "||");
        string[] requiredSkillsAll = QuestUtil.SanitizeAndSplit(requiredSkills.Value!, separator: "&&");

        string requiredSkillsJoined;

        if (requiredSkillsAny.Length >= requiredSkillsAll.Length)
        {
            // Populate an int array with skill levels from the required skills string;
            // replace null values with 1s in case quest maker hasn't specified the skill level

            int?[] requiredSkillsAnyRank = new int?[requiredSkillsAny.Length];
            for (int i = 0; i < requiredSkillsAny.Length; i++)
            {
                requiredSkillsAny[i] = Regex.Match(requiredSkillsAny[i], pattern: @"\d+").Value;
                requiredSkillsAnyRank[i] =
                    TryParse(requiredSkillsAny[i], out int level) ? level : (int?)null ?? 1;
            }

            for (int i = 0; i < requiredSkillsAny.Length; i++)
            {
                NwSkill? requiredSkill = GetRequiredSkill(requiredSkillsAny[i]);

                int? requiredRank = requiredSkillsAnyRank[i];

                if (requiredSkill is null)
                    QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredSkills.Name, requiredSkillsAny[i]);

                if (playerCharacter.GetSkillRank(requiredSkill!) >= requiredRank)
                    return true;
            }

            requiredSkillsJoined = string.Join(separator: "; ", requiredSkillsAny);

            playerCharacter.LoginPlayer!.SendServerMessage
                ($"One of the following skills is required to take this quest: {requiredSkillsJoined}");
            return false;
        }

        int?[] requiredSkillsAllRank = new int?[requiredSkillsAll.Length];
        for (int i = 0; i < requiredSkillsAll.Length; i++)
        {
            requiredSkillsAll[i] = Regex.Match(requiredSkillsAll[i], pattern: @"\d+").Value;
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
            if (playerCharacter.GetSkillRank(requiredSkill!) < requiredRank)
                requiredSkillsList.Add(requiredSkillsAll[i]);
        }

        // If nothing was added to the naughty list, it means that PC has all required skills and quest can be taken 
        if (requiredSkillsList.IsNullOrEmpty()) return true;

        requiredSkillsJoined = string.Join(separator: "; ", requiredSkillsList);

        playerCharacter.LoginPlayer!.SendServerMessage
            ($"The following skills are required to take this quest: {requiredSkillsJoined}");

        return false;

        NwSkill? GetRequiredSkill(string skillRequirementVar)
        {
            Skill skillType = skillRequirementVar switch
            {
                not null when skillRequirementVar.Contains(value: "an", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.AnimalEmpathy,
                not null when skillRequirementVar.Contains(value: "emp", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.AnimalEmpathy,
                not null when skillRequirementVar.Contains(value: "co", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Concentration,
                not null when skillRequirementVar.Contains(value: "disa", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.DisableTrap,
                not null when skillRequirementVar.Contains(value: "disc", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Discipline,
                not null when skillRequirementVar.Contains(value: "he", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Heal,
                not null when skillRequirementVar.Contains(value: "hi", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Hide,
                not null when skillRequirementVar.Contains(value: "lis", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Listen,
                not null when skillRequirementVar.Contains(value: "lor", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.AnimalEmpathy,
                not null when skillRequirementVar.Contains(value: "mov", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.MoveSilently,
                not null when skillRequirementVar.Contains(value: "si", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.MoveSilently,
                not null when skillRequirementVar.Contains(value: "ms", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.MoveSilently,
                not null when skillRequirementVar.Contains(value: "op", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.OpenLock,
                not null when skillRequirementVar.Contains(value: "loc", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.OpenLock,
                not null when skillRequirementVar.Contains(value: "par", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Parry,
                not null when skillRequirementVar.Contains(value: "perf", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Perform,
                not null when skillRequirementVar.Contains(value: "pers", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Persuade,
                not null when skillRequirementVar.Contains(value: "an", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.AnimalEmpathy,
                not null when skillRequirementVar.Contains(value: "an", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.AnimalEmpathy,
                not null when skillRequirementVar.Contains(value: "an", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.AnimalEmpathy,
                not null when skillRequirementVar.Contains(value: "pi", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.PickPocket,
                not null when skillRequirementVar.Contains(value: "poc", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.PickPocket,
                not null when skillRequirementVar.Contains(value: "sea", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Search,
                not null when skillRequirementVar.Contains(value: "set", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.SetTrap,
                not null when skillRequirementVar.Contains(value: "spe", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Spellcraft,
                not null when skillRequirementVar.Contains(value: "spo", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Spot,
                not null when skillRequirementVar.Contains(value: "tau", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Taunt,
                not null when skillRequirementVar.Contains(value: "us", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.UseMagicDevice,
                not null when skillRequirementVar.Contains(value: "mag", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.UseMagicDevice,
                not null when skillRequirementVar.Contains(value: "umd", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.UseMagicDevice,
                not null when skillRequirementVar.Contains(value: "app", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Appraise,
                not null when skillRequirementVar.Contains(value: "tu", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Tumble,
                not null when skillRequirementVar.Contains(value: "craft t", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.CraftTrap,
                not null when skillRequirementVar.Contains(value: "blu", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Bluff,
                not null when skillRequirementVar.Contains(value: "int", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Intimidate,
                not null when skillRequirementVar.Contains(value: "arm", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.CraftArmor,
                not null when skillRequirementVar.Contains(value: "we", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.CraftWeapon,
                not null when skillRequirementVar.Contains(value: "ri", StringComparison.CurrentCultureIgnoreCase)
                    => Skill.Ride,
                _ => Skill.AllSkills
            };
            if (skillType is Skill.AllSkills) return null;

            return NwSkill.FromSkillType(skillType);
        }
    }

    private static bool CheckRequiredAlignments(NwCreature playerCharacter, LocalVariableString requiredAlignments)
    {
        string[] requiredAlignmentsAny = QuestUtil.SanitizeAndSplit(requiredAlignments.Value!, separator: "||");

        foreach (string requiredAlignmentString in requiredAlignmentsAny)
        {
            Alignment? requiredAlignment = GetRequiredAlignment(requiredAlignmentString);

            if (requiredAlignment is null)
            {
                QuestUtil.SendQuestDebug(playerCharacter.LoginPlayer!, requiredAlignments.Name,
                    requiredAlignmentString);
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

        string requiredAlignmentsJoined = string.Join(separator: ", ", requiredAlignmentsAny);

        playerCharacter.LoginPlayer!.SendServerMessage
            ($"This quest requires any of the following alignments:{requiredAlignmentsJoined}");

        return false;

        // Local helper function to keep code more readable and shorter
        Alignment? GetRequiredAlignment(string alignmentRequirementVar)
        {
            return alignmentRequirementVar switch
            {
                not null when alignmentRequirementVar.Contains(value: "go", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Good,
                not null when alignmentRequirementVar.Contains(value: "ev", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Evil,
                not null when alignmentRequirementVar.Contains(value: "ne", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Neutral,
                not null when alignmentRequirementVar.Contains(value: "la", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Lawful,
                not null when alignmentRequirementVar.Contains(value: "tr", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Neutral,
                not null when alignmentRequirementVar.Contains(value: "ch", StringComparison.CurrentCultureIgnoreCase)
                    => Alignment.Chaotic,
                _ => null
            };
        }
    }

    private static bool CheckRequiredClasses(NwCreature playerCharacter, LocalVariableString requiredClasses)
    {
        string[] requiredClassesAny = QuestUtil.SanitizeAndSplit(requiredClasses.Value!, separator: "||");
        string[] requiredClassesAll = QuestUtil.SanitizeAndSplit(requiredClasses.Value!, separator: "&&");

        string requiredClassesJoined;

        if (requiredClassesAny.Length >= requiredClassesAll.Length)
        {
            // Populate an int array with class levels from the required classes string;
            // replace null values with 1s in case quest maker hasn't specified the class level

            int?[] requiredClassesAnyLevels = new int?[requiredClassesAny.Length];
            for (int i = 0; i < requiredClassesAny.Length; i++)
            {
                requiredClassesAny[i] = Regex.Match(requiredClassesAny[i], pattern: @"\d+").Value;
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

            requiredClassesJoined = string.Join(separator: "; ", requiredClassesAny);

            playerCharacter.LoginPlayer!.SendServerMessage
                ($"One of the following classes is required to take this quest: {requiredClassesJoined}");
            return false;
        }

        int?[] requiredClassesAllLevels = new int?[requiredClassesAll.Length];
        for (int i = 0; i < requiredClassesAll.Length; i++)
        {
            requiredClassesAll[i] = Regex.Match(requiredClassesAll[i], pattern: @"\d+").Value;
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

        requiredClassesJoined = string.Join(separator: "; ", requiredClassesList);

        playerCharacter.LoginPlayer!.SendServerMessage
            ($"The following classes are required to take this quest: {requiredClassesJoined}");

        return false;

        // Local helper function to keep the actual code more readable and shorter
        NwClass? GetRequiredClass(string classRequirementVar)
        {
            int classId = classRequirementVar switch
            {
                not null when classRequirementVar.Contains(value: "barb", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_BARBARIAN,
                not null when classRequirementVar.Contains(value: "bard", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_BARD,
                not null when classRequirementVar.Contains(value: "cler", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_CLERIC,
                not null when classRequirementVar.Contains(value: "drui", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DRUID,
                not null when classRequirementVar.Contains(value: "fight", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_FIGHTER,
                not null when classRequirementVar.Contains(value: "monk", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_MONK,
                not null when classRequirementVar.Contains(value: "pala", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_PALADIN,
                not null when classRequirementVar.Contains(value: "rang", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_RANGER,
                not null when classRequirementVar.Contains(value: "rog", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_ROGUE,
                not null when classRequirementVar.Contains(value: "sorc", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_SORCERER,
                not null when classRequirementVar.Contains(value: "wiz", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_WIZARD,
                not null when classRequirementVar.Contains(value: "shif", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_SHIFTER,
                not null when classRequirementVar.Contains(value: "warl", StringComparison.CurrentCultureIgnoreCase)
                    => Warlock,
                not null when classRequirementVar.Contains(value: "ass", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_ASSASSIN,
                not null when classRequirementVar.Contains(value: "arca", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_ARCANE_ARCHER,
                not null when classRequirementVar.Contains(value: "blac", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_BLACKGUARD,
                not null when classRequirementVar.Contains(value: "div", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DIVINE_CHAMPION,
                not null when classRequirementVar.Contains(value: "dra", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DRAGON_DISCIPLE,
                not null when classRequirementVar.Contains(value: "dwar", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_DWARVEN_DEFENDER,
                not null when classRequirementVar.Contains(value: "knig", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_PURPLE_DRAGON_KNIGHT,
                not null when classRequirementVar.Contains(value: "", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_BLACKGUARD,
                not null when classRequirementVar.Contains(value: "pale", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_PALE_MASTER,
                not null when classRequirementVar.Contains(value: "shado", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_SHADOWDANCER,
                not null when classRequirementVar.Contains(value: "weap", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_WEAPON_MASTER,
                not null when classRequirementVar.Contains(value: "scou", StringComparison.CurrentCultureIgnoreCase)
                    => NWScript.CLASS_TYPE_HARPER,
                not null when classRequirementVar.Contains(value: "bow", StringComparison.CurrentCultureIgnoreCase)
                    => BowMaster,
                not null when classRequirementVar.Contains(value: "cav", StringComparison.CurrentCultureIgnoreCase)
                    => Cavalry,
                not null when classRequirementVar.Contains(value: "def", StringComparison.CurrentCultureIgnoreCase)
                    => Defender,
                not null when classRequirementVar.Contains(value: "esc", StringComparison.CurrentCultureIgnoreCase)
                    => EscapeArtist,
                not null when classRequirementVar.Contains(value: "two", StringComparison.CurrentCultureIgnoreCase)
                    => TwoWeaponFighter,
                not null when classRequirementVar.Contains(value: "medi", StringComparison.CurrentCultureIgnoreCase)
                    => CombatMedic,
                not null when classRequirementVar.Contains(value: "arbal", StringComparison.CurrentCultureIgnoreCase)
                    => Arbalest,
                not null when classRequirementVar.Contains(value: "duel", StringComparison.CurrentCultureIgnoreCase)
                    => Duelist,
                not null when classRequirementVar.Contains(value: "lycan", StringComparison.CurrentCultureIgnoreCase)
                    => Lycanthrope,
                not null when classRequirementVar.Contains(value: "peer", StringComparison.CurrentCultureIgnoreCase)
                    => Peerage,
                not null when classRequirementVar.Contains(value: "corr", StringComparison.CurrentCultureIgnoreCase)
                    => FiendishCorrupted,
                not null when classRequirementVar.Contains(value: "bloo", StringComparison.CurrentCultureIgnoreCase)
                    => Bloodsworn,
                _ => -1
            };

            return NwClass.FromClassId(classId);
        }
    }

    /// <summary>
    ///     Checks that the quests required to pick up the new quest are completed
    /// </summary>
    /// <returns>True if required quests are completed, false if not</returns>
    private static bool CheckRequiredQuests(NwCreature playerCharacter, LocalVariableString requiredQuests)
    {
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.ResRef == "ds_pckey");

        // If there is a quest requirement, check that the required quest has been completed to return true and allow
        // character to take on the quest; otherwise play rejection message, inform the player of the quest they need
        // to complete, and return false in the quest script to not start the quest

        string[] requiredQuestsAny = QuestUtil.SanitizeAndSplit(requiredQuests.Value!, separator: "||");
        string[] requiredQuestsAll = QuestUtil.SanitizeAndSplit(requiredQuests.Value!, separator: "&&");

        // If required quests are seperated by "||", character must have completed one of the required quests
        string requiredQuestsJoined;

        if (requiredQuestsAny.Length >= requiredQuestsAll.Length)
        {
            foreach (string requiredQuest in requiredQuestsAny)
            {
                int requiredQuestStatus = pcKey.GetObjectVariable<LocalVariableInt>(requiredQuest).Value;

                if (requiredQuestStatus is QuestCompleted) return true;
            }

            requiredQuestsJoined = string.Join(separator: "; ", requiredQuestsAny).ColorString(ColorConstants.Green);

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

        requiredQuestsJoined = string.Join(separator: "; ", requiredQuestsList).ColorString(ColorConstants.Green);

        playerCharacter.LoginPlayer!.SendServerMessage
            ($"You must complete all of the following quests before you may begin this one: {requiredQuestsJoined}");

        return false;
    }
}