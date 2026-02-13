using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set phenotype on a targeted creature.
/// Ported from f_Pheno() in mod_pla_cmd.nss.
/// Usage: ./pheno &lt;phenotype ID&gt; then click the target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetPhenoCommand : IChatCommand
{
    public string Command => "./pheno";
    public string Description => "Set phenotype on creature (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0 || !int.TryParse(args[0], out int phenoId))
        {
            caller.SendServerMessage("Usage: ./pheno <phenotype ID> then click the target.",
                ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("pheno_pending").Value = phenoId;

        caller.SendServerMessage($"Click on the creature to set phenotype {phenoId}.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int phenoId = dm.GetObjectVariable<LocalVariableInt>("pheno_pending").Value;
        dm.GetObjectVariable<LocalVariableInt>("pheno_pending").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        target.Phenotype = (Phenotype)phenoId;
        obj.Player.SendServerMessage($"Set {target.Name}'s phenotype to {phenoId}.", ColorConstants.Lime);
    }
}
