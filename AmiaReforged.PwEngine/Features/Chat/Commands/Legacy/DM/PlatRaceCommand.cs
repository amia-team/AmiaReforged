using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Authorizes a platinum race for a targeted PC by creating a platinum_token item.
/// Ported from f_PlatRace() in mod_pla_cmd.nss.
///
/// The NWScript version set PCKEY variables (SUBRACE_AUTHORIZED, ds_subrace_activated, ds_done)
/// and called Leto functions. The C# subrace system uses a platinum_token item for authorization,
/// so this command creates one on the target PC.
///
/// Usage: ./platrace authorise   (then click a PC)
///        ./platrace             (shows help)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PlatRaceCommand : IChatCommand
{
    private static readonly HashSet<string> ValidPlatinumRaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "elfling",
        "faerie",
        "shadow",
        "aquatic",
        "snow"
    };

    public string Command => "./platrace";
    public string Description => "Authorise a platinum race for a PC";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        if (args.Length == 0 || args[0].Equals("?", StringComparison.OrdinalIgnoreCase)
                             || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            caller.SendServerMessage(
                "Usage: ./platrace authorise (then click a PC)\n" +
                "Authorises a platinum race. Valid subraces: elfling, faerie, shadow, aquatic, snow.\n" +
                "This creates a platinum_token on the PC and boots them to apply the subrace.",
                ColorConstants.Lime);
            return;
        }

        if (!args[0].Equals("authorise", StringComparison.OrdinalIgnoreCase)
            && !args[0].Equals("authorize", StringComparison.OrdinalIgnoreCase))
        {
            caller.SendServerMessage("Unknown option. Use: ./platrace authorise", ColorConstants.Red);
            return;
        }

        caller.SendServerMessage("Click a PC to authorise their platinum race.", ColorConstants.Lime);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwPlayer caller = obj.Player;
        NwCreature? target = obj.TargetObject as NwCreature;

        if (target == null || !target.IsPlayerControlled)
        {
            caller.SendServerMessage("Target must be a player character.", ColorConstants.Red);
            return;
        }

        string subrace = target.SubRace.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(subrace) || !ValidPlatinumRaces.Contains(subrace))
        {
            caller.SendServerMessage(
                $"{target.Name}'s subrace '{target.SubRace}' is not a valid platinum race. " +
                $"Valid: elfling, faerie, shadow, aquatic, snow.",
                ColorConstants.Red);
            return;
        }

        // Check if they already have a platinum_token
        bool hasToken = NWScript.GetItemPossessedBy(target, "platinum_token") != NWScript.OBJECT_INVALID;

        if (hasToken)
        {
            caller.SendServerMessage($"{target.Name} already has a platinum_token.", ColorConstants.Orange);
            return;
        }

        // Create the platinum_token item to authorize the subrace
        NWScript.CreateItemOnObject("platinum_token", target);

        caller.SendServerMessage(
            $"Authorising {target.Name}'s subrace ({target.SubRace}). " +
            $"Platinum token created. Booting player to apply...",
            ColorConstants.Lime);

        NWScript.FloatingTextStringOnCreature("Your platinum race has been authorised. Reconnecting...",
            target, NWScript.FALSE);

        // Boot the player after a delay so they can read the message
        async void BootAfterDelay()
        {
            await NwTask.Delay(TimeSpan.FromSeconds(6));
            target.ControllingPlayer?.BootPlayer("Your platinum race has been authorised. Please reconnect.");
        }
        BootAfterDelay();
    }
}
