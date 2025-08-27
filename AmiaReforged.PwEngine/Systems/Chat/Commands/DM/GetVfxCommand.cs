using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class GetVfx : IChatCommand
{
    public string Command => "./getvfx";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (!caller.IsDM && environment == "live")
        {
            caller.SendServerMessage
                ($"Only DMs can use {Command} on the live server. You can use this on the test server.");

            return Task.CompletedTask;
        }

        caller.EnterTargetMode(
            targetingData => GetDurVfx(targetingData, caller),
            new TargetModeSettings { ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door });

        return Task.CompletedTask;
    }

    private void GetDurVfx(ModuleEvents.OnPlayerTarget targetingData, NwPlayer caller)
    {
        if (targetingData.TargetObject is not (NwCreature or NwDoor or NwPlaceable)) return;

        Dictionary<int, string> vfxIdAndLabel = [];

        NwGameObject targetObject = (NwGameObject)targetingData.TargetObject;

        foreach (Effect effect in targetObject.ActiveEffects)
        {
            if (effect.EffectType != EffectType.VisualEffect) continue;

            int vfxId = NwGameTables.VisualEffectTable[effect.IntParams[0]].RowIndex;
            string? vfxLabel = NwGameTables.VisualEffectTable[effect.IntParams[0]].Label;


            if (vfxLabel == null) continue;

            vfxIdAndLabel.Add(vfxId, vfxLabel);
        }

        if (vfxIdAndLabel.Count == 0)
        {
            caller.FloatingTextString($"No visual effects found on {targetObject.Name}!");
            return;
        }

        string vfxList = "";

        foreach (KeyValuePair<int, string> kvp in vfxIdAndLabel)
        {
            vfxList += $"ID: {kvp.Key.ToString()}".ColorString(ColorConstants.Yellow);
            vfxList += $"\n{kvp.Value}\n\n";
        }

        _ = PrintVfxList(vfxList, caller, targetObject.Name);
    }

    private async Task PrintVfxList(string vfxList, NwPlayer caller, string targetObjectName)
    {
        NwCreature? controlledCreature = caller.ControlledCreature;
        if (controlledCreature?.Location == null) return;

        NwPlaceable? helperObject = NwPlaceable.Create(template: "x2_plc_psheet", controlledCreature.Location);
        if (helperObject == null)
        {
            caller.SendServerMessage("Helper object failed to materialize.");
            return;
        }

        helperObject.Name = $"{targetObjectName}'s Visual Effects";
        helperObject.PortraitResRef = "po_plc_x0_tme_";
        helperObject.Description = vfxList;

        await NwTask.Delay(TimeSpan.FromMilliseconds(50));

        caller.ForceExamine(helperObject);

        await NwTask.Delay(TimeSpan.FromMilliseconds(50));

        await helperObject.WaitForObjectContext();
        helperObject.Destroy();
    }
}
