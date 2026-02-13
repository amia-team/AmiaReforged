using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Search online players by various criteria.
/// Ported from f_FindPC() in mod_pla_cmd.nss.
/// Usage: ./findpc &lt;criterion&gt; &lt;search term&gt;
/// Criteria: account, align, area, deity, level, name, race
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class FindPcCommand : IChatCommand
{
    public string Command => "./findpc";
    public string Description => "Search PCs: account/align/area/deity/level/name/race <term>";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length < 2)
        {
            ShowHelp(caller);
            return;
        }

        string criterion = args[0].ToLowerInvariant();
        string searchTerm = string.Join(" ", args[1..]).ToLowerInvariant();

        if (criterion is "?" or "help")
        {
            ShowHelp(caller);
            return;
        }

        caller.SendServerMessage($"=== FindPC: {criterion} = '{searchTerm}' ===", ColorConstants.Cyan);
        int count = 0;

        foreach (NwPlayer player in NwModule.Instance.Players)
        {
            NwCreature? creature = player.LoginCreature;
            if (creature == null) continue;

            bool match = criterion switch
            {
                "account" => player.PlayerName.ToLowerInvariant().Contains(searchTerm),
                "area" => (creature.Area?.Name ?? "").ToLowerInvariant().Contains(searchTerm),
                "deity" => NWScript.GetDeity(creature).ToLowerInvariant().Contains(searchTerm),
                "level" => creature.Level.ToString() == searchTerm,
                "name" => creature.Name.ToLowerInvariant().Contains(searchTerm),
                "race" => creature.Race.Name.ToString().ToLowerInvariant().Contains(searchTerm),
                "align" => GetAlignmentString(creature).ToLowerInvariant().Contains(searchTerm),
                _ => false
            };

            if (match)
            {
                string areaName = creature.Area?.Name ?? "Unknown";
                caller.SendServerMessage(
                    $"  {creature.Name} ({player.PlayerName}) - Level {creature.Level}, " +
                    $"Area: {areaName}, Deity: {NWScript.GetDeity(creature)}",
                    ColorConstants.White);
                count++;
            }
        }

        if (count == 0 && !IsValidCriterion(criterion))
        {
            caller.SendServerMessage(
                $"Unknown criterion '{criterion}'. Valid: account, align, area, deity, level, name, race",
                ColorConstants.Orange);
            return;
        }

        caller.SendServerMessage($"Found {count} match(es).", ColorConstants.Gray);
    }

    private static string GetAlignmentString(NwCreature creature)
    {
        int geVal = NWScript.GetAlignmentGoodEvil(creature);
        int lcVal = NWScript.GetAlignmentLawChaos(creature);
        string ge = geVal > 50 ? "Good" :
            geVal < 50 ? "Evil" : "Neutral";
        string lc = lcVal > 50 ? "Lawful" :
            lcVal < 50 ? "Chaotic" : "Neutral";
        return $"{lc} {ge}";
    }

    private static bool IsValidCriterion(string criterion)
    {
        return criterion is "account" or "align" or "area" or "deity" or "level" or "name" or "race";
    }

    private static void ShowHelp(NwPlayer caller)
    {
        caller.SendServerMessage("=== FindPC Command ===", ColorConstants.Cyan);
        caller.SendServerMessage("Search online players by a criterion.", ColorConstants.White);
        caller.SendServerMessage("Usage: ./findpc <criterion> <search term>", ColorConstants.White);
        caller.SendServerMessage("Criteria: account, align, area, deity, level, name, race", ColorConstants.Yellow);
        caller.SendServerMessage("Example: ./findpc area cordor", ColorConstants.White);
    }
}
