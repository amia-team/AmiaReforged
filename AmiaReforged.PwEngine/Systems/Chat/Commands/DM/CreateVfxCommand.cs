using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class CreateVfx : IChatCommand
{
    public string Command => "./createvfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM) return Task.CompletedTask;
        try
        {
            int vfxId = int.Parse(args[0]);
            string vfxType = NwGameTables.VisualEffectTable[vfxId].TypeFd;
            string vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
            caller.ControlledCreature.GetObjectVariable<LocalVariableInt>(name: "createvfxid").Value = vfxId;
            if (args[1] != string.Empty)
            {
                _ = float.TryParse(args[1], out float vfxScale);
                caller.ControlledCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").Value =
                    vfxScale;
            }

            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "D")
            {
                caller.EnterTargetMode(CreateDurVfx,
                    new() { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
                caller.FloatingTextString(
                    $"Creating duration-type visual effect {vfxLabel}. The effect is permanent! You can remove it with \"./removevfx\".",
                    false);
                return Task.CompletedTask;
            }

            if (NwGameTables.VisualEffectTable[vfxId].TypeFd == "F")
            {
                caller.EnterTargetMode(CreateFnfVfx);
                caller.FloatingTextString($"Creating instant-type visual effect {vfxLabel}.", false);
                return Task.CompletedTask;
            }
        }
        catch
        {
            caller.SendServerMessage(
                message:
                "Usage: \"./createvfx <vfx id>\". Optionally, set the vfx scale with \"./createvfx <vfx id> <scale float>\". Use \"./listvfx\" to list vfxs.");
        }

        return Task.CompletedTask;
    }

    private void CreateDurVfx(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature playerCreature = obj.Player.ControlledCreature;
        int vfxId = playerCreature.GetObjectVariable<LocalVariableInt>(name: "createvfxid").Value;
        float vfxScale;
        VisualEffectTableEntry vfx = NwGameTables.VisualEffectTable[vfxId];
        if (obj.TargetObject is NwCreature targetCreature)
        {
            if (playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").HasValue)
                vfxScale = playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").Value;
            else vfxScale = targetCreature.VisualTransform.Scale;
            Effect durVfx = Effect.VisualEffect(vfx, false, vfxScale);
            durVfx.SubType = EffectSubType.Unyielding;
            durVfx.Tag = "dm_persistentvfx";
            targetCreature.ApplyEffect(EffectDuration.Permanent, durVfx);
        }

        if (obj.TargetObject is NwDoor targetDoor)
        {
            if (playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").HasValue)
                vfxScale = playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").Value;
            else vfxScale = targetDoor.VisualTransform.Scale;
            Effect durVfx = Effect.VisualEffect(vfx, false, vfxScale);
            durVfx.SubType = EffectSubType.Unyielding;
            durVfx.Tag = "dm_persistentvfx";
            targetDoor.ApplyEffect(EffectDuration.Permanent, durVfx);
        }

        if (obj.TargetObject is NwPlaceable targetPlaceable)
        {
            if (playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").HasValue)
                vfxScale = playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").Value;
            else vfxScale = targetPlaceable.VisualTransform.Scale;
            Effect durVfx = Effect.VisualEffect(vfx, false, vfxScale);
            durVfx.SubType = EffectSubType.Unyielding;
            durVfx.Tag = "dm_persistentvfx";
            targetPlaceable.ApplyEffect(EffectDuration.Permanent, durVfx);
        }

        playerCreature.GetObjectVariable<LocalVariableInt>(name: "createvfxid").Delete();
        if (playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").HasValue)
            playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").Delete();
    }

    private void CreateFnfVfx(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature playerCreature = obj.Player.ControlledCreature;
        int vfxId = playerCreature.GetObjectVariable<LocalVariableInt>(name: "createvfxid").Value;
        float vfxScale;
        if (playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").HasValue)
            vfxScale = playerCreature.GetObjectVariable<LocalVariableFloat>(name: "createvfxscale").Value;
        else vfxScale = 1f;
        VisualEffectTableEntry vfx = NwGameTables.VisualEffectTable[vfxId];
        NwArea currentArea = obj.Player.ControlledCreature.Area;
        Location targetLocation = Location.Create(currentArea, obj.TargetPosition, 0);

        if (obj.TargetObject is NwCreature creature)
            creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfx, false, vfxScale));
        else targetLocation.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(vfx, false, vfxScale));
    }
}