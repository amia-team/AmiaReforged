using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Give a targeted creature a horn widget item.
/// Ported from f_Horns() in mod_pla_cmd.nss.
/// Usage: ./horns &lt;type&gt; then click the target
/// Types: meph, ox, rothe, balor, antlers, drag, ram
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class HornsCommand : IChatCommand
{
    public string Command => "./horns";
    public string Description => "Give creature horn widget: <type> (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0 || args[0] is "?" or "help")
        {
            ShowHelp(caller);
            return;
        }

        string hornType = args[0].ToLowerInvariant();

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("horns_type").Value = hornType;

        caller.SendServerMessage("Click on the creature.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string hornType = dm.GetObjectVariable<LocalVariableString>("horns_type").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("horns_type").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        int vfxId = HornVfxHelper.GetHornVfx(target, hornType);
        if (vfxId == -1)
        {
            obj.Player.SendServerMessage(
                $"Invalid horn type '{hornType}' for this race. Available: {HornVfxHelper.GetAvailableTypes()}",
                ColorConstants.Orange);
            return;
        }

        uint hornItemId = NWScript.CreateItemOnObject("td_horns", target);
        if (hornItemId == NWScript.OBJECT_INVALID)
        {
            obj.Player.SendServerMessage("Failed to create horn widget (blueprint not found).",
                ColorConstants.Orange);
            return;
        }

        NWScript.SetLocalInt(hornItemId, "td_horn", vfxId);
        NWScript.SetName(hornItemId, $"Effect: {hornType} horns");
        NWScript.SetDescription(hornItemId, $"{target.Name}'s {hornType} horns.");
        obj.Player.SendServerMessage(
            $"Granted {target.Name} {hornType} horns.", ColorConstants.Lime);
    }

    private static void ShowHelp(NwPlayer caller)
    {
        caller.SendServerMessage("=== Horns Command ===", ColorConstants.Cyan);
        caller.SendServerMessage("Usage: ./horns <type> then click the creature", ColorConstants.White);
        caller.SendServerMessage($"Types: {HornVfxHelper.GetAvailableTypes()}", ColorConstants.Yellow);
        caller.SendServerMessage("Note: Not available for Gnome, Halfling, or HalfOrc.", ColorConstants.Gray);
    }
}
