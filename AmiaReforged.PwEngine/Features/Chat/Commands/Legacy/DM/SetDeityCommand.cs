using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set deity on a targeted creature, or search online players by deity.
/// Ported from f_Deity() in mod_pla_cmd.nss.
/// Usage: ./deity find &lt;name&gt; | ./deity &lt;deity name&gt; then click target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetDeityCommand : IChatCommand
{
    public string Command => "./deity";
    public string Description => "Set deity on target or search PCs by deity";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./deity <deity name> (then click target) or ./deity find <name>",
                ColorConstants.Orange);
            return;
        }

        // Handle search mode
        if (args[0].Equals("find", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
        {
            string searchTerm = string.Join(" ", args[1..]).ToLowerInvariant();
            SearchByDeity(caller, searchTerm);
            return;
        }

        // Set deity mode
        string deityName = string.Join(" ", args);
        NwCreature? dmCreature = caller.ControlledCreature;
        if (dmCreature == null) return;

        dmCreature.GetObjectVariable<LocalVariableString>("deity_pending").Value = deityName;

        caller.SendServerMessage("Click on the target creature to set its deity.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string deity = dm.GetObjectVariable<LocalVariableString>("deity_pending").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("deity_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        NWScript.SetDeity(target, deity);
        obj.Player.SendServerMessage($"Set {target.Name}'s deity to '{deity}'.", ColorConstants.Lime);
    }

    private static void SearchByDeity(NwPlayer caller, string searchTerm)
    {
        caller.SendServerMessage($"=== Players with deity matching '{searchTerm}' ===", ColorConstants.Cyan);
        int count = 0;

        foreach (NwPlayer player in NwModule.Instance.Players)
        {
            NwCreature? creature = player.LoginCreature;
            if (creature == null) continue;

            string deity = NWScript.GetDeity(creature).ToLowerInvariant();
            if (deity.Contains(searchTerm))
            {
                caller.SendServerMessage(
                    $"  {creature.Name} - Deity: {NWScript.GetDeity(creature)} (Level {creature.Level})",
                    ColorConstants.White);
                count++;
            }
        }

        caller.SendServerMessage($"Found {count} match(es).", ColorConstants.Gray);
    }
}
