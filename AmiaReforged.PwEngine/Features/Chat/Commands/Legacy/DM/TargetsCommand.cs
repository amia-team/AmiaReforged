using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Show the nearest targetable objects (PC, NPC, PLC, Item) near the DM.
/// Ported from f_Targets() in mod_pla_cmd.nss.
/// Usage: ./targets
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class TargetsCommand : IChatCommand
{
    public string Command => "./targets";
    public string Description => "Show nearest targetable objects";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        caller.SendServerMessage("=== Nearest Targets ===", ColorConstants.Cyan);

        // Nearest PC
        NwCreature? nearestPc = dm.GetNearestCreatures()
            .FirstOrDefault(c => c.IsPlayerControlled);
        caller.SendServerMessage(
            $"  PC: {(nearestPc != null ? nearestPc.Name : "None")}",
            ColorConstants.White);

        // Nearest NPC
        NwCreature? nearestNpc = dm.GetNearestCreatures()
            .FirstOrDefault(c => !c.IsPlayerControlled);
        caller.SendServerMessage(
            $"  NPC: {(nearestNpc != null ? nearestNpc.Name : "None")}",
            ColorConstants.White);

        // Nearest usable PLC (iterate until we find one with usable flag)
        string plcName = "None";
        for (int i = 1; i <= 10; i++)
        {
            NwPlaceable? plc = NWScript.GetNearestObject(NWScript.OBJECT_TYPE_PLACEABLE, dm, i)
                .ToNwObject<NwPlaceable>();
            if (plc == null) break;
            if (plc.Useable)
            {
                plcName = plc.Name;
                break;
            }
        }

        caller.SendServerMessage($"  PLC (usable): {plcName}", ColorConstants.White);

        // Nearest Item
        NwItem? nearestItem = NWScript.GetNearestObject(NWScript.OBJECT_TYPE_ITEM, dm)
            .ToNwObject<NwItem>();
        caller.SendServerMessage(
            $"  Item: {(nearestItem != null ? nearestItem.Name : "None")}",
            ColorConstants.White);
    }
}
