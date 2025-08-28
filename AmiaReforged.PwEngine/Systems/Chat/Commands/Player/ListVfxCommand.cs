using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]
public class ListVfxCommand : IChatCommand
{
    public string Command => "./listvfx";
    private const string UsageMessage
        = "Available inputs for ./listvfx are:" +
          "\n'duration' for duration type visuals" +
          "\n'instant' for instant type visuals" +
          "\n'beam' for beam type visuals" +
          "\n'projectile' for projectile type visuals" +
          "\n'all' for all visual effects";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            caller.SendServerMessage(UsageMessage);
        }

        switch (args[0])
        {
            case "instant" or "inst" or "fnf":
                ListVfx("F", caller);
                break;
            case "duration" or "dur" or "d":
                ListVfx("D", caller);
                break;
            case "projectile" or "proj" or "p":
                ListVfx("P", caller);
                break;
            case "beam" or "b":
                ListVfx("B", caller);
                break;
            case "all":
                ListVfx("all", caller);
                break;
            default:
                caller.SendServerMessage(UsageMessage);
                break;
        }

        return Task.CompletedTask;
    }

    private void ListVfx(string? vfxType, NwPlayer caller)
    {
        string? vfxLabel;
        int vfxId;
        string vfxList = "";

        switch (vfxType)
        {
            case "F" or "D" or "P" or "B":
                for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
                {
                    if (NwGameTables.VisualEffectTable[i].TypeFd != vfxType) continue;

                    vfxLabel = NwGameTables.VisualEffectTable[i].Label?.ToLower();
                    vfxId = NwGameTables.VisualEffectTable.GetRow(i).RowIndex;

                    vfxList += $"ID: {vfxId}".ColorString(ColorConstants.Yellow) +
                               $"\n{vfxLabel}\n";
                }

                break;

            case "all":
                for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
                {
                    vfxLabel = NwGameTables.VisualEffectTable[i].Label?.ToLower();
                    vfxId = NwGameTables.VisualEffectTable.GetRow(i).RowIndex;

                    vfxList += $"ID: {vfxId}".ColorString(ColorConstants.Yellow) +
                               $"\n{vfxLabel}\n";
                }

                break;

            default: caller.SendServerMessage(UsageMessage);
                return;
        }

        _ = PrintVfxList(vfxList, vfxType, caller);
    }

    private async Task PrintVfxList(string vfxList, string vfxType, NwPlayer caller)
    {
        NwCreature? controlledCreature = caller.ControlledCreature;
        if (controlledCreature?.Location == null) return;

        vfxType = vfxType switch
        {
            "F" => "Instant",
            "D" => "Duration",
            "P" => "Projectile",
            "B" => "Beam",
            "all" => "All",
            _ => "All"
        };

        NwPlaceable? helperObject = NwPlaceable.Create(template: "x2_plc_psheet", controlledCreature.Location);
        if (helperObject == null)
        {
            caller.SendServerMessage("Helper object failed to materialize.");
            return;
        }

        helperObject.Description = vfxList;
        helperObject.Name = $"List of {vfxType} Visual Effects";
        helperObject.PortraitResRef = "po_plc_x0_tme_";

        await NwTask.Delay(TimeSpan.FromMilliseconds(50));

        caller.ForceExamine(helperObject);

        await NwTask.Delay(TimeSpan.FromMilliseconds(50));

        await helperObject.WaitForObjectContext();
        helperObject.Destroy();
    }
}
