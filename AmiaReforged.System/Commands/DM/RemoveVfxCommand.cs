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

        if (obj.TargetObject is NwCreature targetCreature)
        foreach (Effect effect in targetCreature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
                if (effect.IntParams[0] == vfxId) targetCreature.RemoveEffect(effect);
        }
        
        if (obj.TargetObject is NwDoor targetDoor)
        foreach (Effect effect in targetDoor.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
                if (effect.IntParams[0] == vfxId) targetDoor.RemoveEffect(effect);
        }
        
        if (obj.TargetObject is NwPlaceable targetPlaceable)
        foreach (Effect effect in targetPlaceable.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
                if (effect.IntParams[0] == vfxId) targetPlaceable.RemoveEffect(effect);
        }
    }
}