using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]
public class ListVfx : IChatCommand
{
    public string Command => "./listvfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        string vfxList = "ID  LABEL";
        int vfxId;
        string vfxLabel;

        
        if (args[0].Contains(value: "inst"))
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "F") continue;

                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }

        if (args[0].Contains(value: "dur"))
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "D") continue;

                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }

        if (args[0].Contains(value: "proj"))
            for (int i = 0; i < NwGameTables.VisualEffectTable.RowCount; i++)
            {
                if (NwGameTables.VisualEffectTable[i].TypeFd != "P") continue;

                vfxLabel = NwGameTables.VisualEffectTable[i].Label.ToLower();
                vfxId = NwGameTables.VisualEffectTable[i].RowIndex;
                vfxList += $"\n{vfxId} {vfxLabel}";
            }

        if (args[0].Contains(value: "beam"))
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
                message: "Usage: \"./listvfx <vfx type>\". Valid types: duration, instant, projectile, beam.");
            return Task.CompletedTask;
        }

        NwPlaceable helperObject = NwPlaceable.Create(template: "ds_invis_obje001", caller.ControlledCreature.Location);
        helperObject.Description = vfxList;
        caller.ActionExamine(helperObject);
        helperObject.Destroy();
        return Task.CompletedTask;
    }
}