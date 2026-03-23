using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// Sets the tag of a targeted object.
/// Usage: ./tag &lt;string&gt; then click the target.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class SetTagCommand : IChatCommand
{
    public string Command => "./tag";
    public string Description => "Set the tag of a targeted object";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length == 0)
        {
            caller.SendServerMessage("Usage: ./tag <new tag> then click the target.",
                ColorConstants.Orange);
            return;
        }

        string newTag = string.Join("_", args);
        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("settag_pending").Value = newTag;

        caller.SendServerMessage($"Click on the target to set its tag to \"{newTag}\".", ColorConstants.Cyan);
        caller.EnterTargetMode(OnTargetSelected,
            new TargetModeSettings
            {
                ValidTargets = ObjectTypes.All
            });
    }

    private static void OnTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature? dm = obj.Player.ControlledCreature;
        if (dm == null) return;

        string newTag = dm.GetObjectVariable<LocalVariableString>("settag_pending").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("settag_pending").Delete();

        if (obj.TargetObject is not NwGameObject target)
        {
            obj.Player.SendServerMessage("Invalid target.", ColorConstants.Orange);
            return;
        }

        string oldTag = target.Tag;
        target.Tag = newTag;

        obj.Player.SendServerMessage(
            $"Tag changed: \"{oldTag}\" -> \"{newTag}\" on {target.Name}.",
            ColorConstants.Lime);
    }
}
