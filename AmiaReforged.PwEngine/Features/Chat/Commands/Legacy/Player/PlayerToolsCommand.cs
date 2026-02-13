using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Adds the Associate Tool feat (1106) to the calling player after a 5-second delay.
/// Ported from f_playertools() in mod_pla_cmd.nss.
/// Usage: ./playertools
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PlayerToolsCommand : IChatCommand
{
    private const int AssociateToolFeatId = 1106;

    public string Command => "./playertools";
    public string Description => "Add the Associate Tool feat to yourself";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? pc = caller.ControlledCreature;
        if (pc == null) return;

        // DMs get a different message
        if (caller.IsDM || caller.IsPlayerDM)
        {
            caller.SendServerMessage("More features will come soon.", ColorConstants.Lime);
            return;
        }

        // Check if they already have the feat
        if (pc.KnowsFeat(NwFeat.FromFeatId(AssociateToolFeatId)!))
        {
            caller.SendServerMessage("You already have the Associate Tool feat.", ColorConstants.Lime);
            return;
        }

        // Add the feat after a 5 second delay, matching the NWScript behavior
        await NwTask.Delay(TimeSpan.FromSeconds(5));
        CreaturePlugin.AddFeat(pc, AssociateToolFeatId);
        caller.SendServerMessage("~ Associate Tool Added ~", ColorConstants.Lime);
    }
}
