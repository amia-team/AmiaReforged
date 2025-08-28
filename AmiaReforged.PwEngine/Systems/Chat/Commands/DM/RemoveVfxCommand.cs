using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class RemoveVfxCommand : IChatCommand
{
    public string Command => "./removevfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        string usageMessage = $"Available inputs for {Command} are:" +
                              "\nVFX ID to remove a specific visual effect" +
                              "\n'all' to remove all visual effects" +
                              "\nTo produce a list of visual effects and their IDs, use ./listvfx" +
                              "\nTo see what visuals a creature or object has, use ./getvfx";

        if (!caller.IsDM && environment == "live")
        {
            caller.SendServerMessage
                ($"Only DMs can use {Command} on the live server. You can use this on the test server.");

            return Task.CompletedTask;
        }

        if (args.Length == 0)
        {
            caller.SendServerMessage(usageMessage);
            return Task.CompletedTask;
        }

        if (args[0] == "all")
        {
            caller.EnterTargetMode(
                targetingData => RemoveVfx(targetingData, null),
                new TargetModeSettings
                    { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door }
            );

            caller.FloatingTextString("Removing all visual effects!", false);

            return Task.CompletedTask;
        }

        if (!int.TryParse(args[0], out int vfxId))
        {
            caller.SendServerMessage("Input not recognised, use a number or 'all'.\n"+usageMessage);

            return Task.CompletedTask;
        }

        string vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label ?? "UNKNOWN";

        switch (NwGameTables.VisualEffectTable[vfxId].TypeFd)
        {
            case "D":
                caller.EnterTargetMode(
                    targetingData => RemoveVfx(targetingData, vfxId),
                    new TargetModeSettings
                        { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door }
                );
                caller.FloatingTextString($"Removing {vfxLabel}!", false);
                break;

            case "F":
                caller.SendServerMessage($"Selected vfx {vfxLabel}, which is an instant-type vfx. {usageMessage}");
                break;

            default:
                caller.SendServerMessage($"Selected vfx {vfxLabel} type is unrecognised. {usageMessage}");
                break;
        }

        return Task.CompletedTask;
    }

    private void RemoveVfx(ModuleEvents.OnPlayerTarget targetingData, int? vfxId)
    {
        if (targetingData.TargetObject is not (NwCreature or NwDoor or NwPlaceable)) return;

        NwGameObject targetObject = (NwGameObject)targetingData.TargetObject;

        if (vfxId != null)
        {
            foreach (Effect effect in targetObject.ActiveEffects)
            {
                if (effect.EffectType != EffectType.VisualEffect) continue;
                if (effect.IntParams[0] != vfxId) continue;

                targetObject.RemoveEffect(effect);
            }

            return;
        }

        foreach (Effect effect in targetObject.ActiveEffects)
        {
            if (effect.EffectType != EffectType.VisualEffect) continue;

            targetObject.RemoveEffect(effect);
        }
    }
}
