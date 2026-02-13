using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set a PC's first or last name permanently. Requires character export and reboot.
/// Ported from f_PCName() in mod_pla_cmd.nss.
/// Usage: ./pcname first &lt;name&gt; | ./pcname last &lt;name&gt; then click the PC
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PcNameCommand : IChatCommand
{
    public string Command => "./pcname";
    public string Description => "Set PC first/last name: first/last <name> (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length < 2)
        {
            caller.SendServerMessage("Usage: ./pcname first <name> or ./pcname last <name>",
                ColorConstants.Orange);
            return;
        }

        string nameType = args[0].ToLowerInvariant();
        if (nameType is not ("first" or "last"))
        {
            caller.SendServerMessage("First argument must be 'first' or 'last'.", ColorConstants.Orange);
            return;
        }

        string newName = string.Join(" ", args[1..]);
        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("pcname_type").Value = nameType;
        dm.GetObjectVariable<LocalVariableString>("pcname_value").Value = newName;

        caller.SendServerMessage("Click on the PC to rename.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static async void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string nameType = dm.GetObjectVariable<LocalVariableString>("pcname_type").Value ?? "";
        string newName = dm.GetObjectVariable<LocalVariableString>("pcname_value").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("pcname_type").Delete();
        dm.GetObjectVariable<LocalVariableString>("pcname_value").Delete();

        if (obj.TargetObject is not NwCreature target || !target.IsPlayerControlled(out NwPlayer? targetPlayer))
        {
            obj.Player.SendServerMessage("Target is not a player character.", ColorConstants.Orange);
            return;
        }

        bool isLastName = nameType == "last";

        // Set the name using NWNX
        CreaturePlugin.SetOriginalName(target, newName, isLastName ? 1 : 0);

        // Export the character
        targetPlayer.ExportCharacter();

        obj.Player.SendServerMessage(
            $"Set {target.Name}'s {nameType} name to '{newName}'. Player will be booted in 6 seconds to apply.",
            ColorConstants.Lime);
        targetPlayer.SendServerMessage(
            $"A DM has changed your {nameType} name. You will be disconnected shortly to apply the change.",
            ColorConstants.Yellow);

        // Boot the player after a delay to apply the name change
        await NwTask.Delay(TimeSpan.FromSeconds(6));
        await NwTask.SwitchToMainThread();

        targetPlayer.BootPlayer("Name change applied. Please reconnect.");
    }
}
