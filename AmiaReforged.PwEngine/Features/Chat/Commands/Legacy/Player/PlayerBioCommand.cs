using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Edit your own character bio/description.
/// Ported from f_bio (player context) / f_PlayerBio() in mod_pla_cmd.nss.
/// Usage: ./bio r (reset) | ./bio b (line break) | ./bio n &lt;text&gt; (new) | ./bio a &lt;text&gt; (append)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PlayerBioCommand : IChatCommand
{
    public string Command => "./bio";
    public string Description => "Edit your own bio: r(eset), b(reak), n(ew) <text>, a(ppend) <text>";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        if (args.Length == 0)
        {
            ShowHelp(caller);
            return;
        }

        string option = args[0].ToLowerInvariant();
        string value = args.Length > 1 ? string.Join(" ", args[1..]) : "";

        switch (option)
        {
            case "r":
            case "reset":
                creature.Description = "";
                caller.SendServerMessage("Bio reset to original.", ColorConstants.Lime);
                break;

            case "b":
            case "break":
                creature.Description += "\n";
                caller.SendServerMessage("Line break added to bio.", ColorConstants.Lime);
                break;

            case "n":
            case "new":
                if (string.IsNullOrWhiteSpace(value))
                {
                    caller.SendServerMessage("Usage: ./bio n <new bio text>", ColorConstants.Orange);
                    return;
                }
                creature.Description = value + " ";
                caller.SendServerMessage("Bio updated.", ColorConstants.Lime);
                break;

            case "a":
            case "append":
                if (string.IsNullOrWhiteSpace(value))
                {
                    caller.SendServerMessage("Usage: ./bio a <text to append>", ColorConstants.Orange);
                    return;
                }
                creature.Description += value + " ";
                caller.SendServerMessage("Text appended to bio.", ColorConstants.Lime);
                break;

            case "?":
            case "help":
                ShowHelp(caller);
                break;

            default:
                ShowHelp(caller);
                break;
        }
    }

    private static void ShowHelp(NwPlayer caller)
    {
        caller.SendServerMessage("=== Bio Command ===", ColorConstants.Cyan);
        caller.SendServerMessage("  ./bio r        - Reset bio to original", ColorConstants.White);
        caller.SendServerMessage("  ./bio b        - Add a line break", ColorConstants.White);
        caller.SendServerMessage("  ./bio n <text> - Set new bio text", ColorConstants.White);
        caller.SendServerMessage("  ./bio a <text> - Append text to bio", ColorConstants.White);
    }
}
