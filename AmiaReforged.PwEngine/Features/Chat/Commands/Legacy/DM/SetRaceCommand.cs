using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set racial type on a targeted PC.
/// Ported from f_race() in mod_pla_cmd.nss.
/// Usage: ./setrace &lt;race ID&gt; then click the PC
/// Valid IDs: 0-20, 23-25, 29
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetRaceCommand : IChatCommand
{
    private static readonly HashSet<int> ValidRaceIds = new()
    {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        23, 24, 25, 29
    };

    public string Command => "./setrace";
    public string Description => "Set racial type on PC: <race ID> (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0 || !int.TryParse(args[0], out int raceId))
        {
            caller.SendServerMessage(
                "Usage: ./setrace <race ID> then click the PC. Valid IDs: 0-20, 23-25, 29",
                ColorConstants.Orange);
            return;
        }

        if (!ValidRaceIds.Contains(raceId))
        {
            caller.SendServerMessage(
                $"Invalid race ID {raceId}. Valid IDs: 0-20, 23-25, 29",
                ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("setrace_pending").Value = raceId;

        caller.SendServerMessage($"Click on the PC to set race {raceId}.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int raceId = dm.GetObjectVariable<LocalVariableInt>("setrace_pending").Value;
        dm.GetObjectVariable<LocalVariableInt>("setrace_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        int oldRace = (int)target.Race.RacialType;
        CreaturePlugin.SetRacialType(target, raceId);
        obj.Player.SendServerMessage(
            $"Set {target.Name}'s racial type from {oldRace} to {raceId}.", ColorConstants.Lime);
    }
}
