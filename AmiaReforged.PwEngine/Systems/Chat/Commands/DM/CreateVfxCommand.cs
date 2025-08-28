using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class CreateVfxCommand : IChatCommand
{
    public string Command => "./createvfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (!caller.IsDM && environment == "live")
        {
            caller.SendServerMessage
                ($"Only DMs can use {Command} on the live server. You can use this on the test server.");

            return Task.CompletedTask;
        }

        string usageMessage = $"Available inputs for {Command} are:" +
                              "\nFirst parameter: VFX ID to create a specific vfx." +
                              "\nSecond parameter (optional): a float value for the size of the vfx." +
                              "\nTo produce a list of visual effects and their IDs, use ./listvfx" +
                              "\nTo see what visuals a creature or object has, use ./getvfx";

        if (args.Length < 1 || !int.TryParse(args[0], out int vfxId))
        {
            caller.SendServerMessage($"Invalid or missing VFX ID, it must be an integer." +
                                     $"\n{usageMessage}");

            return Task.CompletedTask;
        }

        float vfxSize = 1;

        if (args.Length >= 2)
        {
            if (!float.TryParse(args[1], out float size))
            {
                caller.SendServerMessage($"Invalid value for {args[1]}, must be a float or empty." +
                                         $"\n{usageMessage}");

                return Task.CompletedTask;
            }

            vfxSize = size;
        }

        CreateVfx(vfxId, vfxSize, caller);

        return Task.CompletedTask;
    }

    private void CreateVfx(int vfxId, float vfxSize, NwPlayer caller)
    {
        string? vfxType = NwGameTables.VisualEffectTable[vfxId].TypeFd;
        if (vfxType == null)
        {
            caller.SendServerMessage("Invalid VFX ID.");
            return;
        }

        string? vfxLabel = NwGameTables.VisualEffectTable[vfxId].Label;
        if (vfxLabel == null)
        {
            caller.SendServerMessage("Label not found for this VFX ID.");
            vfxLabel = "UNKNOWN VFX";
        }

        switch (vfxType)
        {
            case "D":
                caller.EnterTargetMode(targetingData => CreateDurVfx(targetingData, vfxId, vfxSize),
                    new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
                caller.FloatingTextString($"Applying duration VFX: {vfxLabel}. The effect is permanent! " +
                                          "You can remove it with ./removevfx", false);
                break;

            case "F":
                caller.EnterTargetMode(targetingData => CreateFnfVfx(targetingData, vfxId, vfxSize));
                caller.FloatingTextString($"Applying instant VFX: {vfxLabel}", false);
                break;

            case "B" or "P":
                caller.EnterTargetMode(targetingData => CreateBeamVfx(targetingData, vfxId),
                    new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });
                caller.FloatingTextString($"Applying beam VFX: {vfxLabel}", false);
                break;
        }
    }

    private void CreateDurVfx(ModuleEvents.OnPlayerTarget targetingData, int vfxId, float vfxSize)
    {
        if (targetingData.TargetObject is not (NwCreature or NwDoor or NwPlaceable)) return;

        NwGameObject targetObject = (NwGameObject)targetingData.TargetObject;

        Effect durationVfx = Effect.VisualEffect((VfxType)vfxId, fScale: vfxSize);

        if (targetingData.Player.IsDM)
            durationVfx.SubType = EffectSubType.Unyielding;

        targetObject.ApplyEffect(EffectDuration.Permanent, durationVfx);
    }

    private void CreateFnfVfx(ModuleEvents.OnPlayerTarget targetingData, int vfxId, float vfxSize)
    {
        Effect instantVfx = Effect.VisualEffect((VfxType)vfxId, fScale: vfxSize);

        if (targetingData.TargetObject is NwCreature or NwDoor or NwPlaceable)
        {
            NwGameObject targetObject = (NwGameObject)targetingData.TargetObject;

            targetObject.ApplyEffect(EffectDuration.Instant, instantVfx);
        }

        NwArea? area = targetingData.Player.ControlledCreature?.Area;
        if (area == null) return;

        Location targetLocation = Location.Create(area, targetingData.TargetPosition, 1f);

        targetLocation.ApplyEffect(EffectDuration.Instant, instantVfx);
    }

    private void CreateBeamVfx(ModuleEvents.OnPlayerTarget targetingData, int vfxId)
    {
        if (targetingData.TargetObject is not (NwCreature or NwDoor or NwPlaceable)) return;
        if (targetingData.Player.ControlledCreature is not { } playerCreature) return;

        NwGameObject targetObject = (NwGameObject)targetingData.TargetObject;

        Effect beamVfx = Effect.Beam((VfxType)vfxId, playerCreature, BodyNode.Hand);

        targetObject.ApplyEffect(EffectDuration.Temporary, beamVfx, NwTimeSpan.FromRounds(1));
    }
}
