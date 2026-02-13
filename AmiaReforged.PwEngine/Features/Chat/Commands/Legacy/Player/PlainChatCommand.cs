using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Toggles plain chat mode (disables emote/OOC color formatting).
/// Ported from f_Plain() in mod_pla_cmd.nss.
/// Usage: ./plain (toggles on/off)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PlainChatCommand : IChatCommand
{
    public string Command => "./plain";
    public string Description => "Toggle plain chat mode (no color formatting)";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        LocalVariableInt plainVar = creature.GetObjectVariable<LocalVariableInt>("CHAT_PLAIN");
        bool isCurrentlyPlain = plainVar.Value != 0;

        if (isCurrentlyPlain)
        {
            plainVar.Value = 0;
            caller.SendServerMessage("Plain chat mode disabled. Color formatting restored.", ColorConstants.Lime);
        }
        else
        {
            plainVar.Value = 1;
            caller.SendServerMessage("Plain chat mode enabled. No color formatting will be applied.", ColorConstants.Yellow);
        }
    }
}
