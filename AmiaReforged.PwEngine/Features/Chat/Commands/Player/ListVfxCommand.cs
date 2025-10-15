using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Player;

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
          "\n'all' for all visual effects" +
          "\nor query for matches with your own search word";

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
                ListVfx(args[0], caller);
                break;
        }

        return Task.CompletedTask;
    }

    private void ListVfx(string vfxType, NwPlayer caller)
    {
        string vfxList = vfxType switch
        {
            "F" or "D" or "P" or "B" => string.Join("\n",
                NwGameTables.VisualEffectTable.Rows.Where(row => row.TypeFd == vfxType)
                    .Select(row =>
                        $"ID: {row.RowIndex}".ColorString(ColorConstants.Yellow) + $"\n{row.Label?.ToLower()}")),

            "all" => string.Join("\n",
                NwGameTables.VisualEffectTable.Rows.Select(row =>
                    $"ID: {row.RowIndex}".ColorString(ColorConstants.Yellow) + $"\n{row.Label?.ToLower()}")),

            _ => string.Join("\n",
                NwGameTables.VisualEffectTable.Rows
                    .Where(row =>
                        row.Label?.ToLower().Contains(vfxType, StringComparison.CurrentCultureIgnoreCase) == true)
                    .Select(row =>
                        $"ID: {row.RowIndex}".ColorString(ColorConstants.Yellow) + $"\n{row.Label?.ToLower()}"))
        };

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
            _ => $"Queried '{vfxType}'"
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
