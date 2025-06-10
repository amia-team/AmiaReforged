using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class RemoveVfx : IChatCommand
{
    public string Command => "./removevfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM) return Task.CompletedTask;
        try
        {
            int vfxId = int.Parse(args[0]);
            string? vfxType = NwGameTables.VisualEffectTable[vfxId].TypeFd;
            string? vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
            NwCreature? controlledCreature = caller.ControlledCreature;
            if(controlledCreature is null) return Task.CompletedTask;
            controlledCreature.GetObjectVariable<LocalVariableInt>(name: "createvfxid").Value = vfxId;
            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "D")
            {
                caller.EnterTargetMode(RemoveDurVfx,
                    new() { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
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
                message:
                "Usage: \"./removevfx <vfx id>\". Use \"./listvfx dur\" to list valid vfxs. Use \"./getvfx\" to get vfxs on the target.");
        }

        return Task.CompletedTask;
    }

    private void RemoveDurVfx(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? playerControlledCreature = obj.Player.ControlledCreature;
        
        if (playerControlledCreature is null) return;
        
        int vfxId = playerControlledCreature.GetObjectVariable<LocalVariableInt>(name: "createvfxid").Value;

        if (obj.TargetObject is NwCreature targetCreature)
            foreach (Effect effect in targetCreature.ActiveEffects)
            {
                if (effect.EffectType == EffectType.VisualEffect)
                    if (effect.IntParams[0] == vfxId)
                        targetCreature.RemoveEffect(effect);
            }

        if (obj.TargetObject is NwDoor targetDoor)
            foreach (Effect effect in targetDoor.ActiveEffects)
            {
                if (effect.EffectType == EffectType.VisualEffect)
                    if (effect.IntParams[0] == vfxId)
                        targetDoor.RemoveEffect(effect);
            }

        if (obj.TargetObject is NwPlaceable targetPlaceable)
            foreach (Effect effect in targetPlaceable.ActiveEffects)
            {
                if (effect.EffectType == EffectType.VisualEffect)
                    if (effect.IntParams[0] == vfxId)
                        targetPlaceable.RemoveEffect(effect);
            }
    }
}