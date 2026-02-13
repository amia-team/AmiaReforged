using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Add a feat by ID to a targeted creature.
/// Ported from f_AddFeat() in mod_pla_cmd.nss.
/// Usage: ./addfeat &lt;feat ID&gt; then click the target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class AddFeatCommand : IChatCommand
{
    public string Command => "./addfeat";
    public string Description => "Add feat by ID to creature (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0 || !int.TryParse(args[0], out int featId))
        {
            caller.SendServerMessage("Usage: ./addfeat <feat ID> then click the target.", ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("addfeat_pending").Value = featId;

        caller.SendServerMessage($"Click on the creature to add feat {featId}.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int featId = dm.GetObjectVariable<LocalVariableInt>("addfeat_pending").Value;
        dm.GetObjectVariable<LocalVariableInt>("addfeat_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        CreaturePlugin.AddFeatByLevel(target, featId, 1);
        obj.Player.SendServerMessage($"Added feat {featId} to {target.Name}.", ColorConstants.Lime);
    }
}
