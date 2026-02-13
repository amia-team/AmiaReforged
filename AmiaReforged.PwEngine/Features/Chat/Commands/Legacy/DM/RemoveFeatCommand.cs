using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Remove a feat by ID from a targeted creature.
/// Ported from f_RemoveFeat() in mod_pla_cmd.nss.
/// Usage: ./removefeat &lt;feat ID&gt; then click the target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class RemoveFeatCommand : IChatCommand
{
    public string Command => "./removefeat";
    public string Description => "Remove feat by ID from creature (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0 || !int.TryParse(args[0], out int featId))
        {
            caller.SendServerMessage("Usage: ./removefeat <feat ID> then click the target.",
                ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("removefeat_pending").Value = featId;

        caller.SendServerMessage($"Click on the creature to remove feat {featId}.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int featId = dm.GetObjectVariable<LocalVariableInt>("removefeat_pending").Value;
        dm.GetObjectVariable<LocalVariableInt>("removefeat_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        CreaturePlugin.RemoveFeat(target, featId);
        obj.Player.SendServerMessage($"Removed feat {featId} from {target.Name}.", ColorConstants.Lime);
    }
}
