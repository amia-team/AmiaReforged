using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Create a deity ring on a targeted creature from the database.
/// Ported from f_Ring() / CreateRing() in mod_pla_cmd.nss / inc_ds_records.nss.
/// Usage: ./ring [deity name] then click the target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class DeityRingCommand : IChatCommand
{
    public string Command => "./ring";
    public string Description => "Create deity ring on target: [deity name] (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        string deitySearch = args.Length > 0 ? string.Join(" ", args) : "";

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("ring_deity").Value = deitySearch;

        caller.SendServerMessage("Click on the creature to create a deity ring.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string deity = dm.GetObjectVariable<LocalVariableString>("ring_deity").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("ring_deity").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        // Query the rings table (matching original CreateRing() from inc_ds_records.nss)
        string sql = string.IsNullOrEmpty(deity)
            ? "SELECT item_data FROM rings ORDER BY RAND() LIMIT 1"
            : $"SELECT item_data FROM rings WHERE item_name LIKE '%{deity}%' LIMIT 1";

        NWScript.SetLocalString(NwModule.Instance, "NWNX!ODBC!SETSCORCOSQL", sql);
        NWScript.RetrieveCampaignObject("NWNX", "-", target.Location!, target);

        string desc = string.IsNullOrEmpty(deity) ? "random" : deity;
        obj.Player.SendServerMessage(
            $"Created {desc} deity ring on {target.Name}.", ColorConstants.Lime);
    }
}
