using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]

public class ListVfx : IChatCommand
{
    public string Command => $"./listvfx";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        string vfxList = "ID  LABEL";
        int vfxId;
        string vfxLabel;

        if (message.Split(' ')[1].Contains("inst"))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "F") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        if (message.Split(' ')[1].Contains("dur"))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "D") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        if (message.Split(' ')[1].Contains("proj"))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "P") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        if (message.Split(' ')[1].Contains("beam"))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "B") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        else 
        {
            caller.SendServerMessage("Usage: \"./listvfx <vfx type>\". Valid types: duration, instant, projectile, beam.");
            return Task.CompletedTask;
        }

        NwPlaceable helperObject = NwPlaceable.Create("ds_invis_obje001", caller.ControlledCreature.Location);
        helperObject.Description = vfxList;
        caller.ActionExamine(helperObject);
        helperObject.Destroy();
        return Task.CompletedTask;
    }
}