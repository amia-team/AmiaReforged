using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]

public class GetVfx : IChatCommand
{
    public string Command => $"./getvfx";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if (!caller.IsDM) return Task.CompletedTask;

        caller.EnterTargetMode(GetDurVfx, new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
        return Task.CompletedTask;
    }
    
    private void GetDurVfx(ModuleEvents.OnPlayerTarget obj)
    {
        int vfxId;
        int vfxIndex = 0;
        string vfxLabel;
        string vfxList = "All visual effects on the target object:\n";
        if (obj.TargetObject is NwCreature targetCreature)
        foreach (Effect effect in targetCreature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
            {
                vfxId = effect.IntParams[0];
                vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
                vfxIndex++;
                vfxList += $"\n{vfxIndex}. ID: {vfxId}. Label: {vfxLabel}\n";
            }
        }
        if (obj.TargetObject is NwDoor targetDoor)
        foreach (Effect effect in targetDoor.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
            {
                vfxId = effect.IntParams[0];
                vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
                vfxIndex++;
                vfxList += $"\n{vfxIndex}. ID: {vfxId}. Label: {vfxLabel}\n";
            }
        }
        if (obj.TargetObject is NwPlaceable targetPlaceable)
        foreach (Effect effect in targetPlaceable.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
            {
                vfxId = effect.IntParams[0];
                vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
                vfxIndex++;
                vfxList += $"\n{vfxIndex}. ID: {vfxId}. Label: {vfxLabel}\n";
            }
        }
        NwPlaceable helperObject = NwPlaceable.Create("ds_invis_obje001", obj.Player.ControlledCreature.Location);
        helperObject.Name = $"List of visual effects on {obj.TargetObject.Name}";
        helperObject.Description = vfxList;
        obj.Player.ActionExamine(helperObject);
        helperObject.Destroy();
    }
}