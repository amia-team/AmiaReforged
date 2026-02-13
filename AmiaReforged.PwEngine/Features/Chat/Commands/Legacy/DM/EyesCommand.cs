using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.DM;

/// <summary>
/// Give a targeted creature glowing eyes via VFX.
/// Ported from f_Eyes() in mod_pla_cmd.nss.
/// Usage: ./eyes t|p &lt;color&gt; then click the target
/// t = temporary (VFX effect), p = permanent (widget item)
/// Colors: cyan, green, orange, purple, red, white, yellow, blue, negred
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class EyesCommand : IChatCommand
{
    public string Command => "./eyes";
    public string Description => "Give creature glowing eyes: t/p <color> (click to target)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM && !caller.IsPlayerDM) return;
        await NwTask.SwitchToMainThread();

        if (args.Length < 2)
        {
            ShowHelp(caller);
            return;
        }

        if (args[0] is "?" or "help")
        {
            ShowHelp(caller);
            return;
        }

        string mode = args[0].ToLowerInvariant();
        if (mode is not ("t" or "p"))
        {
            caller.SendServerMessage("First argument must be 't' (temporary) or 'p' (permanent).",
                ColorConstants.Orange);
            return;
        }

        string color = string.Join(" ", args[1..]).Trim();

        NwCreature? dm = caller.ControlledCreature;
        if (dm == null) return;

        dm.GetObjectVariable<LocalVariableString>("eyes_mode").Value = mode;
        dm.GetObjectVariable<LocalVariableString>("eyes_color").Value = color;

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

        string mode = dm.GetObjectVariable<LocalVariableString>("eyes_mode").Value ?? "";
        string color = dm.GetObjectVariable<LocalVariableString>("eyes_color").Value ?? "";
        dm.GetObjectVariable<LocalVariableString>("eyes_mode").Delete();
        dm.GetObjectVariable<LocalVariableString>("eyes_color").Delete();

        if (obj.TargetObject is not NwCreature target)
        {
            obj.Player.SendServerMessage("Target is not a creature.", ColorConstants.Orange);
            return;
        }

        int vfxId = EyeVfxHelper.GetEyeVfx(target, color);
        if (vfxId == -1)
        {
            obj.Player.SendServerMessage(
                $"Invalid eye color '{color}' for this race/gender. Available: {EyeVfxHelper.GetAvailableColors()}",
                ColorConstants.Orange);
            return;
        }

        if (mode == "t")
        {
            // Temporary - apply as supernatural VFX effect
            Effect eyeEffect = Effect.VisualEffect((VfxType)vfxId);
            eyeEffect.SubType = EffectSubType.Supernatural;
            target.ApplyEffect(EffectDuration.Permanent, eyeEffect);
            obj.Player.SendServerMessage(
                $"Applied temporary {color} eyes to {target.Name}.", ColorConstants.Lime);
        }
        else
        {
            // Permanent - create eye widget item
            uint eyeItemId = NWScript.CreateItemOnObject("td_eyes", target);
            if (eyeItemId == NWScript.OBJECT_INVALID)
            {
                obj.Player.SendServerMessage("Failed to create eye widget (blueprint not found).",
                    ColorConstants.Orange);
                return;
            }

            NWScript.SetLocalInt(eyeItemId, "td_color", vfxId);
            NWScript.SetName(eyeItemId, $"Effect: {color} eyes");
            NWScript.SetDescription(eyeItemId, $"{target.Name}'s {color} glowing eyes.");
            obj.Player.SendServerMessage(
                $"Created permanent {color} eye widget on {target.Name}.", ColorConstants.Lime);
        }
    }

    private static void ShowHelp(NwPlayer caller)
    {
        caller.SendServerMessage("=== Eyes Command ===", ColorConstants.Cyan);
        caller.SendServerMessage("Usage: ./eyes <t|p> <color> then click the creature", ColorConstants.White);
        caller.SendServerMessage("  t = temporary (VFX, lost on death/DM heal/reset)", ColorConstants.White);
        caller.SendServerMessage("  p = permanent (widget item)", ColorConstants.White);
        caller.SendServerMessage($"Colors: {EyeVfxHelper.GetAvailableColors()}", ColorConstants.Yellow);
        caller.SendServerMessage("Example: ./eyes p cyan", ColorConstants.White);
    }
}
