using System.Runtime.InteropServices;
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
        if (message.Split(' ').Length <= 1)
        {
            caller.SendServerMessage(
                "Usage: \"./listvfx <vfx type>\". Recognised types: duration, instant, projectile, beam.");
            return Task.CompletedTask;
        }
        
        string vfxList = "ID  LABEL";

        string vfxLabel;
        int vfxId;

        string paramInput = message.Split(' ')[1];
        string[] durParams = {"d", "dur", "duration"};
        string[] fnfParams = {"f", "fnf", "i", "inst", "instant"};
        string[] projParams = {"p", "proj", "projectile"};
        string[] beamParams = {"b", "beam"};
        
        if (durParams.Contains(paramInput))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "D") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        if (fnfParams.Contains(paramInput))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "F") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        if (projParams.Contains(paramInput))
        {
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "P") continue;
            
                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }
        }
        if (beamParams.Contains(paramInput))
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
            caller.SendServerMessage(
                "Usage: \"./listvfx <vfx type>\". Recognised types: duration, instant, projectile, beam.");
            return Task.CompletedTask;
        }
        
        NwPlaceable helperObject = NwPlaceable.Create("ds_invis_obje001", caller.ControlledCreature.Location);
        helperObject.Description = vfxList;
        caller.ActionExamine(helperObject);
        helperObject.Destroy();
        return Task.CompletedTask;
    }
}