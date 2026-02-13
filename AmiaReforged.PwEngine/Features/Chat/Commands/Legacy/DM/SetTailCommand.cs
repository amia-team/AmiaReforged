using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set tail type on a targeted creature, or search available tails.
/// Ported from f_Tail() in mod_pla_cmd.nss.
/// Usage: ./tail &lt;id&gt; then click target | ./tail find &lt;search term&gt;
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetTailCommand : IChatCommand
{
    public string Command => "./tail";
    public string Description => "Set tail on creature: <id> (click) or find <term>";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./tail <id> then click target, or ./tail find <search term>",
                ColorConstants.Orange);
            return;
        }

        // Search mode
        if (args[0].Equals("find", StringComparison.OrdinalIgnoreCase))
        {
            string search = args.Length > 1 ? string.Join(" ", args[1..]).ToLowerInvariant() : "";
            SearchTails(caller, search);
            return;
        }

        if (!int.TryParse(args[0], out int tailId))
        {
            caller.SendServerMessage("Tail ID must be a number.", ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("tail_pending").Value = tailId;

        caller.SendServerMessage($"Click on the creature to set tail {tailId}.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int tailId = dm.GetObjectVariable<LocalVariableInt>("tail_pending").Value;
        dm.GetObjectVariable<LocalVariableInt>("tail_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        target.TailType = (CreatureTailType)tailId;
        obj.Player.SendServerMessage($"Set {target.Name}'s tail to {tailId}.", ColorConstants.Lime);
    }

    private static void SearchTails(NwPlayer caller, string search)
    {
        caller.SendServerMessage($"=== Tail Search: '{search}' ===", ColorConstants.Cyan);
        int count = 0;

        // Search through 2DA tail entries
        for (int i = 0; i <= 490; i++)
        {
            string label = NWScript.Get2DAString("tailmodel", "LABEL", i);
            if (string.IsNullOrEmpty(label)) continue;
            if (!string.IsNullOrEmpty(search) && !label.ToLowerInvariant().Contains(search)) continue;

            caller.SendServerMessage($"  [{i}] {label}", ColorConstants.White);
            count++;

            if (count >= 30)
            {
                caller.SendServerMessage("  ... (results truncated, refine search)", ColorConstants.Gray);
                break;
            }
        }

        caller.SendServerMessage($"Found {count} result(s).", ColorConstants.Gray);
    }
}
