using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Set a targeted creature as friendly to a standard faction (reputation to 100).
/// Ported from f_Love() in mod_pla_cmd.nss.
/// Usage: ./love H|C|M|D then click the target
/// H=Hostile, C=Commoner, M=Merchant, D=Defender
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class LoveCommand : IChatCommand
{
    public string Command => "./love";
    public string Description => "Make target loved by faction: H/C/M/D (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./love <H|C|M|D> then click the target. " +
                                     "H=Hostile, C=Commoner, M=Merchant, D=Defender", ColorConstants.Orange);
            return;
        }

        string factionCode = args[0].ToUpperInvariant();
        int factionId = FactionHelper.GetFactionId(factionCode);

        if (factionId == -1)
        {
            caller.SendServerMessage("Invalid faction. Use H, C, M, or D.", ColorConstants.Orange);
            return;
        }

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableInt>("love_faction").Value = factionId;
        dm.GetObjectVariable<LocalVariableString>("love_faction_name").Value =
            FactionHelper.GetFactionName(factionCode);

        caller.SendServerMessage("Click on the creature.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        int factionId = dm.GetObjectVariable<LocalVariableInt>("love_faction").Value;
        string factionName = dm.GetObjectVariable<LocalVariableString>("love_faction_name").Value ?? "";
        dm.GetObjectVariable<LocalVariableInt>("love_faction").Delete();
        dm.GetObjectVariable<LocalVariableString>("love_faction_name").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        NWScript.SetStandardFactionReputation(factionId, 100, target);
        obj.Player.SendServerMessage(
            $"Set {target.Name} as friendly to {factionName} faction (rep 100).", ColorConstants.Lime);
    }
}
