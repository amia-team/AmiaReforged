using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]

public class RemoveVfx : IChatCommand
{
    public string Command => $"./removevfx";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if (!caller.IsDM) return Task.CompletedTask;
        try
        {
            int vfxId = int.Parse(message.Split(' ')[1]);
            string vfxType = NwGameTables.VisualEffectTable[vfxId].TypeFd;
            string vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
            caller.ControlledCreature.GetObjectVariable<LocalVariableInt>("createvfxid").Value = vfxId;
            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "D")
            {
                caller.EnterTargetMode(RemoveDurVfx, new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
                caller.FloatingTextString($"Removing {vfxLabel}!", false);
                return Task.CompletedTask;
            }
            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "F")
            {
                caller.SendServerMessage(
                    $"Selected vfx {vfxLabel} which is an instant-type vfx. Use \"./listvfx dur\" to list valid vfxs. Use \"./getvfx\" to get vfxs on the target.");
                return Task.CompletedTask;
            }
        }
        catch 
        {
            caller.SendServerMessage(
                "Usage: \"./removevfx <reference number>\". Use \"./listvfx dur\" to list valid vfxs. Use \"./getvfx\" to get vfxs on the target.");
        }
        return Task.CompletedTask;
    }
    
    private void RemoveDurVfx(ModuleEvents.OnPlayerTarget obj)
    {
        int vfxId = obj.Player.ControlledCreature.GetObjectVariable<LocalVariableInt>("createvfxid").Value;
        VisualEffectTableEntry vfx = NwGameTables.VisualEffectTable[vfxId];
        if (obj.TargetObject is NwCreature targetCreature) targetCreature.RemoveEffect(Effect.VisualEffect(vfx));
        if (obj.TargetObject is NwDoor targetDoor) targetDoor.RemoveEffect(Effect.VisualEffect(vfx));
        if (obj.TargetObject is NwPlaceable targetPlaceable) targetPlaceable.RemoveEffect(Effect.VisualEffect(vfx));
    }
}