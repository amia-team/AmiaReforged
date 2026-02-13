using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Set yourself hostile to all non-party PCs (PvP hostility toggle).
/// Ported from f_Hostile() in mod_pla_cmd.nss.
/// Usage: ./hostile
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class HostileCommand : IChatCommand
{
    public string Command => "./hostile";
    public string Description => "Set yourself hostile to all non-party PCs";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        int count = 0;
        foreach (NwPlayer otherPlayer in NwModule.Instance.Players)
        {
            NwCreature? otherCreature = otherPlayer.LoginCreature;
            if (otherCreature == null || otherCreature == creature) continue;

            // Only set hostile to PCs not in same faction/party
            if (NWScript.GetFactionEqual(otherCreature, creature) == NWScript.FALSE)
            {
                NWScript.SetPCDislike(creature, otherCreature);
                count++;
            }
        }

        caller.SendServerMessage($"You are now hostile to {count} player(s).", ColorConstants.Orange);
    }
}
