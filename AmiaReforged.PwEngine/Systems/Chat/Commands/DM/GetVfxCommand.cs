using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class GetVfx : IChatCommand
{
    public string Command => "./getvfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM) return Task.CompletedTask;

        caller.EnterTargetMode(GetDurVfx,
            new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
        return Task.CompletedTask;
    }

    private async void GetDurVfx(ModuleEvents.OnPlayerTarget obj)
    {
        try
        {
            int vfxId;
            int vfxIndex = 0;
            string? vfxLabel;
            string vfxList = $"Visual effects on {obj.TargetObject?.Name}:\n";
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

            NwCreature? playerControlledCreature = obj.Player.ControlledCreature;
            if(playerControlledCreature is null) return;
            
            if(playerControlledCreature.Location is null) return;
            
            NwPlaceable? helperObject =
                NwPlaceable.Create(template: "ds_invis_object3", playerControlledCreature.Location);
            if(helperObject is null) return;
            
            helperObject.Description = vfxList;
            await obj.Player.ActionExamine(helperObject);
            await NwTask.SwitchToMainThread();
            
            await NwTask.Delay(TimeSpan.FromSeconds(1));
            await NwTask.SwitchToMainThread();
            
            helperObject.Destroy();

        }
        catch (Exception e)
        {
            LogManager.GetCurrentClassLogger().Error(e);
        }
    }
}