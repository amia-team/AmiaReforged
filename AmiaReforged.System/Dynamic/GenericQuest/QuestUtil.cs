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

    public static NwClass? GetRequiredClass(string classRequirementVar)
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