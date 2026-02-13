using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Set yourself friendly to all PCs (removes PvP hostility).
/// Ported from f_Friend() in mod_pla_cmd.nss.
/// Usage: ./friend
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class FriendCommand : IChatCommand
{
    public string Command => "./friend";
    public string Description => "Set yourself friendly to all PCs";
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

        int count = 0;
        foreach (NwPlayer otherPlayer in NwModule.Instance.Players)
        {
            NwCreature? otherCreature = otherPlayer.LoginCreature;
            if (otherCreature == null || otherCreature == creature) continue;

            NWScript.SetPCLike(creature, otherCreature);
            count++;
        }

        caller.SendServerMessage($"You are now friendly to {count} player(s).", ColorConstants.Lime);
    }
}
