using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]

public class CreateVfx : IChatCommand
{
    public string paramVfxId = "";
    public string Command => $"./createvfx {paramVfxId}";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if (caller.IsDM == false) return Task.CompletedTask;
        if (Command[11..].TryParseInt(out int vfxId))
        {
            string vfxType = NwGameTables.VisualEffectTable[vfxId].TypeFd;
            string vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "D")
            {
                caller.EnterTargetMode(CreateDurVfx, new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
                caller.FloatingTextString($"Creating duration-type visual effect {vfxLabel}. The effect is permanent! You can remove it with './removevfx'.", false);
                return Task.CompletedTask;
            }
            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "F")
            {
                caller.EnterTargetMode(CreateFnfVfx);
                caller.FloatingTextString($"Creating instant-type visual effect {vfxLabel}.", false);
                return Task.CompletedTask;
            }
        }
        else 
        {
            caller.SendServerMessage("You're trying to create a visual effect. You must enter a reference number, './createvfx [reference number]'. To view the list, enter './listvfx'.");
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    private void CreateDurVfx(ModuleEvents.OnPlayerTarget obj)
    {
        VisualEffectTableEntry vfxId = NwGameTables.VisualEffectTable[Command[11..].ParseInt()];
        if (obj.TargetObject is NwCreature targetCreature) 
        {
            float targetObjectScale = targetCreature.VisualTransform.Scale;
            Effect durVfx = Effect.VisualEffect(vfxId, false, targetObjectScale);
            durVfx.SubType = EffectSubType.Unyielding;
            targetCreature.ApplyEffect(EffectDuration.Permanent, durVfx);
        }
        if (obj.TargetObject is NwDoor targetDoor)
        {
            float targetObjectScale = targetDoor.VisualTransform.Scale;
            Effect durVfx = Effect.VisualEffect(vfxId, false, targetObjectScale);
            durVfx.SubType = EffectSubType.Unyielding;
            targetDoor.ApplyEffect(EffectDuration.Permanent, durVfx);
        }
        if (obj.TargetObject is NwPlaceable targetPlaceable)
        {
            float targetObjectScale = targetPlaceable.VisualTransform.Scale;
            Effect durVfx = Effect.VisualEffect(vfxId, false, targetObjectScale);
            durVfx.SubType = EffectSubType.Unyielding;
            targetPlaceable.ApplyEffect(EffectDuration.Permanent, durVfx);
        }
    }
    private void CreateFnfVfx(ModuleEvents.OnPlayerTarget obj)
    {
        VisualEffectTableEntry vfxId = NwGameTables.VisualEffectTable[Command[11..].ParseInt()];
        NwArea currentArea = obj.Player.ControlledCreature.Area;
        Location targetLocation = Location.Create(currentArea, obj.TargetPosition, 0);
        targetLocation.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfxId));
    }
}