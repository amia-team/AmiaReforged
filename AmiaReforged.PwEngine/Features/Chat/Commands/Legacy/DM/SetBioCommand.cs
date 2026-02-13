using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set the description/bio on any targeted object (PC, NPC, Item, PLC).
/// Ported from f_Bio() (DM context) in mod_pla_cmd.nss.
/// Usage: ./setbio &lt;text&gt; | ./setbio +&lt;text&gt; (append) | ./setbio (reset)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetBioCommand : IChatCommand
{
    public string Command => "./setbio";
    public string Description => "Set bio on target: <text>, +<text> (append), empty (reset)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        NwCreature? dmCreature = caller.ControlledCreature;
        if (dmCreature == null) return;

        string value = args.Length > 0 ? string.Join(" ", args) : "";

        // Store the value for the target callback
        dmCreature.GetObjectVariable<LocalVariableString>("setbio_value").Value = value;

        caller.SendServerMessage("Click on the target to set its bio.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected);
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string value = dm.GetObjectVariable<LocalVariableString>("setbio_value").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("setbio_value").Delete();

        if (obj.TargetObject is not NwGameObject target)
        {
            obj.Player.SendServerMessage("Invalid target.", ColorConstants.Orange);
            return;
        }

        if (value.StartsWith("+"))
        {
            // Append mode
            string appendText = value[1..];
            target.Description += "\n\n" + appendText;
            obj.Player.SendServerMessage($"Appended to {target.Name}'s bio.", ColorConstants.Lime);
        }
        else if (!string.IsNullOrEmpty(value))
        {
            // Overwrite mode
            target.Description = value;
            obj.Player.SendServerMessage($"Updated {target.Name}'s bio.", ColorConstants.Lime);
        }
        else
        {
            // Reset mode
            target.Description = "";
            obj.Player.SendServerMessage($"Reset {target.Name}'s bio to default.", ColorConstants.Lime);
        }
    }
}
