using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Edit the description of a ground item that has the "js_" tag prefix.
/// Ported from f_jsbio() in mod_pla_cmd.nss.
/// Usage: ./jsbio a|b|n &lt;text&gt; then click the item.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class JsBioCommand : IChatCommand
{
    public string Command => "./jsbio";
    public string Description => "Edit description of 'js_' item: a(ppend)/b(reak)/n(ew) <text>";
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
        if (option is "?" or "help")
        {
            ShowHelp(caller);
            return;
        }

        string value = args.Length > 1 ? string.Join(" ", args[1..]) : "";

        // Store the operation and text temporarily
        creature.GetObjectVariable<LocalVariableString>("jsbio_option").Value = option;
        creature.GetObjectVariable<LocalVariableString>("jsbio_value").Value = value;

        caller.SendServerMessage("Click on the item to edit its description.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Item
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? creature = obj.Player.LoginCreature;
        if (creature == null) return;

        string option = creature.GetObjectVariable<LocalVariableString>("jsbio_option").Value ?? "";
        string value = creature.GetObjectVariable<LocalVariableString>("jsbio_value").Value ?? "";
        creature.GetObjectVariable<LocalVariableString>("jsbio_option").Delete();
        creature.GetObjectVariable<LocalVariableString>("jsbio_value").Delete();

        if (obj.TargetObject is not NwItem item)
        {
            obj.Player.SendServerMessage("Target is not an item.", ColorConstants.Orange);
            return;
        }

        if (!item.Tag.StartsWith("js_"))
        {
            obj.Player.SendServerMessage("You can only edit items with the 'js_' tag prefix.",
                ColorConstants.Orange);
            return;
        }

        switch (option)
        {
            case "a": // Append
                item.Description = item.Description + value;
                obj.Player.SendServerMessage($"Text appended to {item.Name}'s description.", ColorConstants.Lime);
                break;

            case "b": // Line break
                item.Description = item.Description + "\n\n";
                obj.Player.SendServerMessage($"Line break added to {item.Name}'s description.", ColorConstants.Lime);
                break;

            case "n": // New
                item.Description = value;
                obj.Player.SendServerMessage($"{item.Name}'s description updated.", ColorConstants.Lime);
                break;

            default:
                obj.Player.SendServerMessage("Invalid option. Use a(ppend), b(reak), or n(ew).",
                    ColorConstants.Orange);
                break;
        }
    }

    private static void ShowHelp(NwPlayer caller)
    {
        caller.SendServerMessage("=== JsBio Command ===", ColorConstants.Cyan);
        caller.SendServerMessage("Edit the description of a 'js_' tagged item.", ColorConstants.White);
        caller.SendServerMessage("  ./jsbio a <text> - Append text", ColorConstants.White);
        caller.SendServerMessage("  ./jsbio b        - Add line break", ColorConstants.White);
        caller.SendServerMessage("  ./jsbio n <text> - Set new description", ColorConstants.White);
    }
}
