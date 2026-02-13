using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set wing type on a targeted creature, or search available wings.
/// Ported from f_Wings() in mod_pla_cmd.nss.
/// Usage: ./wings &lt;id&gt; then click target | ./wings find &lt;search term&gt;
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetWingsCommand : IChatCommand
{
    public string Command => "./wings";
    public string Description => "Set wings on creature: <id> (click) or find <term>";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./wings <id> then click target, or ./wings find <search term>",
                ColorConstants.Orange);
            return;
        }

        // Search mode
        if (args[0].Equals("find", StringComparison.OrdinalIgnoreCase))
        {
            string search = args.Length > 1 ? string.Join(" ", args[1..]).ToLowerInvariant() : "";
            SearchWings(caller, search);
            return;
        }

        if (!int.TryParse(args[0], out int wingId))
        {
            caller.SendServerMessage("Wing ID must be a number.", ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("wings_pending").Value = wingId;

        caller.SendServerMessage($"Click on the creature to set wings {wingId}.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int wingId = dm.GetObjectVariable<LocalVariableInt>("wings_pending").Value;
        dm.GetObjectVariable<LocalVariableInt>("wings_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        target.WingType = (CreatureWingType)wingId;
        obj.Player.SendServerMessage($"Set {target.Name}'s wings to {wingId}.", ColorConstants.Lime);
    }

    private static void SearchWings(NwPlayer caller, string search)
    {
        caller.SendServerMessage($"=== Wing Search: '{search}' ===", ColorConstants.Cyan);
        int count = 0;

        for (int i = 0; i <= 89; i++)
        {
            string label = NWScript.Get2DAString("wingmodel", "LABEL", i);
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
