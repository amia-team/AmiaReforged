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
        string vfxList = "ID    LABEL";

        string vfxLabel;
        int vfxId;

        for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
        {
            if (NwGameTables.VisualEffectTable[i].TypeFd != "D") continue;
            
            vfxLabel = NwGameTables.VisualEffectTable[i].Label;
            vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
            vfxList += $"\n{vfxId}  {vfxLabel}";
        }

        caller.ControlledCreature.Description = vfxList;
        caller.ActionExamine(caller.ControlledCreature);
        caller.ControlledCreature.Description = originalDescription;
        return Task.CompletedTask;
    }
}