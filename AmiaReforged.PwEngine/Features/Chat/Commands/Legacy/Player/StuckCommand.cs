using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Teleports the player to the nearest spawn waypoint when not in combat.
/// Ported from f_stuck in mod_pla_cmd.nss.
/// Usage: ./stuck
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class StuckCommand : IChatCommand
{
    private const string SpawnpointTag = "ds_spwn";

    public string Command => "./stuck";
    public string Description => "Teleport to nearest spawn waypoint";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        // Block if in combat (matching original script)
        if (creature.IsInCombat)
        {
            caller.SendServerMessage("Cannot use this command while in combat.", ColorConstants.Orange);
            return;
        }

        // Get nearest spawn waypoint
        uint waypoint = NWScript.GetNearestObjectByTag(SpawnpointTag, creature);
        if (waypoint == NWScript.OBJECT_INVALID)
        {
            caller.SendServerMessage("No spawn waypoint found nearby.", ColorConstants.Orange);
            return;
        }

        // Jump to the waypoint
        NWScript.AssignCommand(creature, () => NWScript.ActionJumpToObject(waypoint));
        caller.SendServerMessage("Teleporting to nearest spawn point.", ColorConstants.Lime);
    }
}

