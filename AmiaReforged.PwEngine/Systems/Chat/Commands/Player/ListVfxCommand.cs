using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]
public class ListVfx : IChatCommand
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
            case "instant" when args[0].Contains("inst"):
                ListVfxByType("F", caller);
                break;
            case "duration" when args[0].Contains("dur"):
                ListVfxByType("D", caller);
                break;
            case "projectile" when args[0].Contains("proj"):
                ListVfxByType("P", caller);
                break;
            case "beam":
                ListVfxByType("B", caller);
                break;
            case "all":
                ListVfxByType("all", caller);
                break;
            default:
                caller.SendServerMessage(UsageMessage);
                break;
        }

        return Task.CompletedTask;
    }

    private void ListVfxByType(string? vfxType, NwPlayer caller)
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

        _ = PrintVfxList(vfxList, caller);
    }

    private async Task PrintVfxList(string vfxList, NwPlayer caller)
    {
        NwCreature? controlledCreature = caller.ControlledCreature;
        if (controlledCreature?.Location is null) return;

        NwPlaceable? helperObject = NwPlaceable.Create(template: "ds_invis_obje001", controlledCreature.Location);
        if (helperObject is null) return;

        helperObject.Description = vfxList;
        await caller.ActionExamine(helperObject);

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
        helperObject.Destroy();
    }
}
