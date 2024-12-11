using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]

public class ListVfx : IChatCommand
{
    public string Command => $"./listvfx";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        string originalDescription = caller.ControlledCreature.Description;
        string stringFormat = string.Format("{0, -15} {1, -4}");
        string vfxList = string.Format(stringFormat, "VFX LABEL", "VFX ID");

        string vfxLabel;
        int vfxId;

        for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
        {
            if (NwGameTables.VisualEffectTable[i].TypeFd != "D") continue;
            
            vfxLabel = NwGameTables.VisualEffectTable[i].Label[..15];
            vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
            vfxList += "\n" + string.Format(stringFormat, $"{vfxLabel}, {vfxId}");
        }

        caller.ControlledCreature.Description = vfxList;
        caller.ActionExamine(caller.ControlledCreature);
        caller.ControlledCreature.Description = originalDescription;
        return Task.CompletedTask;
    }
}