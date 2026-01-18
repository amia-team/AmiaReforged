using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Module.DivineClassFeatures;

/// <summary>
/// Validates divine caster status and handles fallen divine characters.
/// Ported from amx_fallcheck.nss
/// </summary>
[ServiceBinding(typeof(FallCheckService))]
public class FallCheckService
{
    private const string FallWidget = "dg_fall";
    private const string FavoredWidget = "ds_favor";

    public FallCheckService()
    {
        NwModule.Instance.OnSpellCast += OnSpellCast;
    }

    private void OnSpellCast(OnSpellCast spellCast)
    {
        if (spellCast.Caster is not NwCreature caster)
            return;

        if (!FallenCastCheck(caster))
        {
            spellCast.PreventSpellCast = true;
        }
    }

    /// <summary>
    /// Checks if a paladin has an exception to worship a non-lawful good deity.
    /// </summary>
    private bool PaladinExceptionCheck(string deity)
    {
        return deity is "Sune" or "Corellon Larethian" or "Selune";
    }

    /// <summary>
    /// Checks if a deity is valid for druids.
    /// </summary>
    private bool DruidCheck(string deity)
    {
        if (string.IsNullOrEmpty(deity))
            return false;

        NwPlaceable? idol = FindIdol(deity);
        if (idol == null)
            return false;

        // Use PrayerService's IsValidDruidGod check
        if (IsValidDruidGod(idol))
            return true;

        // Specific exception check for nature deities that do not have animal/plant domains
        if (deity is "Auril" or "Talos" or "Talona" or "Umberlee" or "Deep Sashelas" or
            "Anhur" or "Istishia" or "Kossuth" or "Grumbar" or "Queen of Air and Darkness" or "Garyx")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the caster has a valid deity for their class.
    /// </summary>
    private bool DeityCheck(NwCreature creature, NwPlayer? player, ClassType classType)
    {
        string deity = NWN.Core.NWScript.GetDeity(creature);

        if (string.IsNullOrEmpty(deity))
        {
            player?.SendServerMessage("You have no deity which you can draw divine power from.");
            return false;
        }

        NwPlaceable? idol = FindIdol(deity);
        if (idol == null)
        {
            player?.SendServerMessage($"{deity} has no domain on Amia...");
            return false;
        }

        // Paladin exception check
        if (classType == ClassType.Paladin && PaladinExceptionCheck(deity))
        {
            return true;
        }

        // Check alignment vs god
        if (!MatchAlignment(creature, idol))
        {
            player?.SendServerMessage("Your alignment and your patron's are out of sync...");
            return false;
        }

        // Check domains for clerics
        if (classType == ClassType.Cleric)
        {
            if (MatchDomain(creature, idol, false) == -1)
            {
                player?.SendServerMessage("Your clerical domains do not align with your patron's...");
                return false;
            }
            if (MatchDomain(creature, idol, true) == -1)
            {
                player?.SendServerMessage("Your clerical domains do not align with your patron's...");
                return false;
            }
        }

        // Check druid validity
        if (classType == ClassType.Druid)
        {
            if (!DruidCheck(deity))
            {
                player?.SendServerMessage("Your patron does not support druidism...");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the caster has a fallen widget or flag.
    /// </summary>
    private bool IsSpecificFallen(NwCreature creature)
    {
        if (NWN.Core.NWScript.GetLocalInt(creature, "Fallen") == 1)
            return true;

        if (creature.FindItemWithTag(FallWidget) != null)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the caster has a favored widget.
    /// </summary>
    private bool IsSpecificFavored(NwCreature creature)
    {
        return creature.FindItemWithTag(FavoredWidget) != null;
    }

    /// <summary>
    /// Main spell cast validation check for divine casters.
    /// </summary>
    private bool FallenCastCheck(NwCreature caster)
    {
        NwPlayer? player = caster.ControllingPlayer;

        // Don't bother checking if it came from an item
        if (NWN.Core.NWScript.GetSpellCastItem() != NWN.Core.NWScript.OBJECT_INVALID)
            return true;

        // If the caster has no PCKey (AKA is an NPC), don't block
        if (caster.FindItemWithTag("ds_pckey") == null)
            return true;

        // DMs aren't blocked
        if (player?.IsDM == true)
            return true;

        // Favored characters bypass checks
        if (IsSpecificFavored(caster))
            return true;

        int spellClass = NWN.Core.NWScript.GetLastSpellCastClass();
        ClassType classType = (ClassType)spellClass;

        // Blackguard alignment check
        if (classType == ClassType.Blackguard)
        {
            if (NWN.Core.NWScript.GetAlignmentGoodEvil(caster) != NWN.Core.NWScript.ALIGNMENT_EVIL)
            {
                player?.SendServerMessage("Your soul is not dark enough to draw blackguard powers.");
                return false;
            }
        }

        // Paladin alignment checks
        if (classType == ClassType.Paladin)
        {
            if (NWN.Core.NWScript.GetAlignmentGoodEvil(caster) != NWN.Core.NWScript.ALIGNMENT_GOOD)
            {
                player?.SendServerMessage("Your soul has too much evil to cast paladin spells.");
                return false;
            }
            if (NWN.Core.NWScript.GetAlignmentLawChaos(caster) != NWN.Core.NWScript.ALIGNMENT_LAWFUL)
            {
                player?.SendServerMessage("Your soul has too much chaos to cast paladin spells.");
                return false;
            }
        }

        // Check for fallen status for all divine classes
        if (classType is ClassType.Blackguard or ClassType.Cleric or ClassType.Druid or
            ClassType.Paladin or ClassType.Ranger or ClassType.DivineChampion)
        {
            if (IsSpecificFallen(caster))
            {
                player?.SendServerMessage("You are fallen and cannot cast divine spells.");
                return false;
            }
        }

        // Deity checks for non-blackguard divine classes
        if (classType is ClassType.Cleric or ClassType.Druid or ClassType.Paladin or
            ClassType.Ranger or ClassType.DivineChampion)
        {
            if (!DeityCheck(caster, player, classType))
            {
                return false;
            }
        }

        return true;
    }

    // Helper methods that mirror PrayerService functionality
    private NwPlaceable? FindIdol(string godName)
    {
        string formattedName = CapitalizeWords(godName);
        if (formattedName == "QueenOfAirAndDarkness")
            formattedName = "QueenofAirandDarkness";

        string idolTag = $"idol2_{formattedName}";

        foreach (NwArea area in NwModule.Instance.Areas)
        {
            NwPlaceable? idol = area.FindObjectsOfTypeInArea<NwPlaceable>()
                .FirstOrDefault(p => p.Tag?.Equals(idolTag, StringComparison.OrdinalIgnoreCase) == true);

            if (idol != null)
                return idol;
        }

        return null;
    }

    private string CapitalizeWords(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string result = "";

        foreach (string word in words)
        {
            if (word.Length > 0)
            {
                result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
        }

        return result;
    }

    private bool MatchAlignment(NwCreature creature, NwPlaceable idol)
    {
        int lawChaos = NWN.Core.NWScript.GetAlignmentLawChaos(creature);
        int goodEvil = NWN.Core.NWScript.GetAlignmentGoodEvil(creature);

        string creatureAlignment = "";

        if (lawChaos == NWN.Core.NWScript.ALIGNMENT_LAWFUL)
            creatureAlignment += "L";
        else if (lawChaos == NWN.Core.NWScript.ALIGNMENT_CHAOTIC)
            creatureAlignment += "C";
        else
            creatureAlignment += "N";

        if (goodEvil == NWN.Core.NWScript.ALIGNMENT_GOOD)
            creatureAlignment += "G";
        else if (goodEvil == NWN.Core.NWScript.ALIGNMENT_EVIL)
            creatureAlignment += "E";
        else
            creatureAlignment += "N";

        string alignmentVar = $"al_{creatureAlignment}";
        int acceptsAlignment = NWN.Core.NWScript.GetLocalInt(idol, sVarName: alignmentVar);

        return acceptsAlignment == 1;
    }

    private int MatchDomain(NwCreature creature, NwPlaceable idol, bool getSecondDomain)
    {
        int pcDomain = NWN.Core.NWScript.GetDomain(creature, getSecondDomain ? 2 : 1);

        if (pcDomain == -1 || pcDomain == 0)
            return -1;

        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWN.Core.NWScript.GetLocalInt(idol, sVarName: $"dom_{i}");
            if (idolDomain == pcDomain)
            {
                return pcDomain;
            }
        }

        return -1;
    }

    private bool IsValidDruidGod(NwPlaceable idol)
    {
        if (NWN.Core.NWScript.GetLocalInt(idol, sVarName: "druid_deity") == 1)
            return true;

        for (int i = 1; i <= 6; i++)
        {
            int idolDomain = NWN.Core.NWScript.GetLocalInt(idol, sVarName: $"dom_{i}");

            // Domain constants: Animal = 1, Plant = 14, Moon = 43, Sun = 17
            if (idolDomain == 1 || idolDomain == 14 || idolDomain == 43 || idolDomain == 17)
            {
                return true;
            }
        }

        return false;
    }
}
