using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Create a sign-spawning widget item on a targeted creature.
/// Ported from f_SignWidget() in mod_pla_cmd.nss.
/// Usage: ./signwidget &lt;sign name&gt; then click the target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SignWidgetCommand : IChatCommand
{
    public string Command => "./signwidget";
    public string Description => "Create sign widget on target (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./signwidget <sign name> then click the target.",
                ColorConstants.Orange);
            return;
        }

        string signName = string.Join(" ", args);
        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("signwidget_name").Value = signName;

        caller.SendServerMessage("Click on the target creature.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature
        });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string signName = dm.GetObjectVariable<LocalVariableString>("signwidget_name").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("signwidget_name").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        // Create the sign widget item from blueprint
        uint widgetId = NWScript.CreateItemOnObject("ds_sign_widget", target);
        if (widgetId == NWScript.OBJECT_INVALID)
        {
            obj.Player.SendServerMessage("Failed to create sign widget (blueprint not found).",
                ColorConstants.Orange);
            return;
        }

        NWScript.SetName(widgetId, signName);
        NWScript.SetDescription(widgetId, "Sign Widget");
        obj.Player.SendServerMessage($"Sign widget '{signName}' created on {target.Name}.", ColorConstants.Lime);
    }
}
