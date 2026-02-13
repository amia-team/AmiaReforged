using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set name on a targeted NPC, Item, or PLC (not PCs — use ./pcname for PCs).
/// Ported from f_Name() in mod_pla_cmd.nss.
/// Usage: ./setname &lt;new name&gt; then click target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetNameCommand : IChatCommand
{
    public string Command => "./setname";
    public string Description => "Set name on NPC/Item/PLC (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./setname <new name> then click the target.",
                ColorConstants.Orange);
            return;
        }

        string newName = string.Join(" ", args);
        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("setname_pending").Value = newName;

        caller.SendServerMessage("Click on the target to rename.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected);
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string newName = dm.GetObjectVariable<LocalVariableString>("setname_pending").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("setname_pending").Delete();

        if (obj.TargetObject is not NwGameObject target)
        {
            obj.Player.SendServerMessage("Invalid target.", ColorConstants.Orange);
            return;
        }

        // For PCs, redirect to ./pcname
        if (target is NwCreature creature && creature.IsPlayerControlled(out _))
        {
            obj.Player.SendServerMessage("Use ./pcname for PC name changes.", ColorConstants.Orange);
            return;
        }

        string oldName = target.Name;
        target.Name = newName;
        obj.Player.SendServerMessage($"Renamed '{oldName}' to '{newName}'.", ColorConstants.Lime);
    }
}
