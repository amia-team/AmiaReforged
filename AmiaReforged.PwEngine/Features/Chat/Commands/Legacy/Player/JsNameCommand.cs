using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Rename a ground item that has the "js_" tag prefix (player-authorized items).
/// Ported from f_jsname() in mod_pla_cmd.nss.
/// Usage: ./jsname &lt;new name&gt; then click the item.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class JsNameCommand : IChatCommand
{
    public string Command => "./jsname";
    public string Description => "Rename a 'js_' prefixed ground item (click to target)";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./jsname <new name> then click the target item.",
                ColorConstants.Orange);
            return;
        }

        string newName = string.Join(" ", args);

        // Store the new name temporarily
        creature.GetObjectVariable<LocalVariableString>("jsname_pending").Value = newName;

        caller.SendServerMessage("Click on the item to rename.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Item
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? creature = obj.Player.LoginCreature;
        if (creature == null) return;

        string newName = creature.GetObjectVariable<LocalVariableString>("jsname_pending").Value ?? "";
        creature.GetObjectVariable<LocalVariableString>("jsname_pending").Delete();

        if (obj.TargetObject is not NwItem item)
        {
            obj.Player.SendServerMessage("Target is not an item.", ColorConstants.Orange);
            return;
        }

        // Validate the item has the "js_" tag prefix
        if (!item.Tag.StartsWith("js_"))
        {
            obj.Player.SendServerMessage("You can only rename items with the 'js_' tag prefix.",
                ColorConstants.Orange);
            return;
        }

        string oldName = item.Name;
        item.Name = newName;
        obj.Player.SendServerMessage($"Renamed '{oldName}' to '{newName}'.", ColorConstants.Lime);
    }
}
