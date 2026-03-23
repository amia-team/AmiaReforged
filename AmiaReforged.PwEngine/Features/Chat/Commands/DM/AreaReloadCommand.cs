using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// Reloads an area by destroying and recreating it from the module resource.
/// Mirrors the behaviour of area_load.nss <c>recreate_area()</c>.
/// Usage: ./area reload &lt;resref&gt;
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class AreaReloadCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static void NotifyAllDMs(string message)
    {
        foreach (NwPlayer player in NwModule.Instance.Players)
        {
            if (player.IsDM || player.IsPlayerDM)
            {
                player.SendServerMessage(message, ColorConstants.Cyan);
            }
        }
    }

    public string Command => "./area";
    public string Description => "Area management commands. Usage: ./area reload <resref>";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length < 2)
        {
            caller.SendServerMessage(
                "Usage: ./area reload <resref>",
                ColorConstants.Orange);
            return;
        }

        string subCommand = args[0].ToLowerInvariant();

        switch (subCommand)
        {
            case "reload":
                string resRef = args[1];
                ReloadArea(caller, resRef);
                break;
            default:
                caller.SendServerMessage(
                    $"Unknown sub-command \"{subCommand}\". Available: reload",
                    ColorConstants.Orange);
                break;
        }
    }

    private static void ReloadArea(NwPlayer caller, string resRef)
    {
        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a =>
            a.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase));

        if (area == null)
        {
            caller.SendServerMessage($"No area found with resref \"{resRef}\".", ColorConstants.Orange);
            NotifyAllDMs($"Area reload failed — resref \"{resRef}\" is invalid.");
            Log.Warn($"Area reload: resref \"{resRef}\" not found.");
            return;
        }

        // Make sure the area is empty of players before destroying it.
        int playerCount = area.Objects.OfType<NwCreature>().Count(c => c.IsPlayerControlled(out _));

        if (playerCount > 0)
        {
            caller.SendServerMessage(
                $"Cannot reload \"{area.Name}\" — {playerCount} player(s) still in the area.",
                ColorConstants.Red);
            NotifyAllDMs($"Could not destroy area \"{area.Name}\" — area not empty.");
            return;
        }

        string areaName = area.Name;

        area.Destroy();
        Log.Info($"Area \"{areaName}\" (resref: {resRef}) destroyed for reload.");

        NwArea? recreated = NwArea.Create(resRef);

        if (recreated != null)
        {
            caller.SendServerMessage(
                $"Area \"{areaName}\" reloaded successfully.",
                ColorConstants.Lime);
            NotifyAllDMs($"Area \"{areaName}\" was reloaded by a DM.");
            Log.Info($"Area \"{areaName}\" (resref: {resRef}) recreated successfully.");
        }
        else
        {
            caller.SendServerMessage(
                $"Destroyed \"{areaName}\" but failed to recreate it. Check the resref.",
                ColorConstants.Red);
            NotifyAllDMs($"WARNING: Area \"{areaName}\" was destroyed but could not be recreated!");
            Log.Error($"Failed to recreate area \"{areaName}\" (resref: {resRef}).");
        }
    }
}
