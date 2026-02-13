using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Make a targeted object speak text.
/// Ported from f_Say() in mod_pla_cmd.nss.
/// Usage: ./say &lt;text&gt; then click the target
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SayCommand : IChatCommand
{
    public string Command => "./say";
    public string Description => "Make target object speak text (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./say <text> then click the target.", ColorConstants.Orange);
            return;
        }

        string text = string.Join(" ", args);
        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("say_pending").Value = text;

        caller.SendServerMessage("Click on the target object.", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected);
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string text = dm.GetObjectVariable<LocalVariableString>("say_pending").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("say_pending").Delete();

        if (obj.TargetObject is not NwGameObject target)
        {
            obj.Player.SendServerMessage("Invalid target.", ColorConstants.Orange);
            return;
        }

        target.SpeakString(text);
        obj.Player.SendServerMessage($"Assigning text to {target.Name}.", ColorConstants.Lime);
    }
}
