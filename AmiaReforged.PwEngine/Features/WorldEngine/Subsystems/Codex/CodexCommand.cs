using AmiaReforged.PwEngine.Features.Chat.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex;

/// <summary>
/// Opens the player codex via <c>./codex</c>. Players only — DMs are rejected.
/// Delegates to <see cref="ICodexSubsystem"/> which encapsulates window lifecycle.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class CodexCommand : IChatCommand
{
    private readonly ICodexSubsystem _codex;

    public CodexCommand(ICodexSubsystem codex)
    {
        _codex = codex;
    }

    public string Command => "./codex";
    public string Description => "Opens your codex; ./codex display reports viewport and GUI scale";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM)
        {
            caller.SendServerMessage("The codex is not available for DMs at this time.", ColorConstants.Orange);
            return;
        }

        if (args.Length > 0 && args[0].Equals("display", StringComparison.OrdinalIgnoreCase))
        {
            int width = caller.GetDeviceProperty(PlayerDeviceProperty.GuiWidth);
            int height = caller.GetDeviceProperty(PlayerDeviceProperty.GuiHeight);
            int scale = caller.GetDeviceProperty(PlayerDeviceProperty.GuiScale);
            caller.SendServerMessage($"Codex display: client GUI {width} x {height}, GUI scale {scale}%. " +
                                     $"Layout {PlayerCodexView.WindowW} x {PlayerCodexView.WindowH} logical units.", ColorConstants.Cyan);
            if (width <= 0 || height <= 0 || scale <= 0)
                caller.SendServerMessage("Client display properties are unavailable. Report your game resolution and GUI scale instead.", ColorConstants.Cyan);
            return;
        }

        // Toggle journal-linked codex off
        if (args.Length > 0 && args[0].Equals("no-journal", StringComparison.OrdinalIgnoreCase))
        {
            NwCreature? creature = caller.LoginCreature;
            if (creature == null) return;

            creature.GetObjectVariable<LocalVariableInt>("no_journal").Value = 1;
            caller.SendServerMessage("Codex will no longer open with the journal. Use ./codex to open it directly.", ColorConstants.Lime);
            return;
        }

        // Toggle journal-linked codex on
        if (args.Length > 0 && args[0].Equals("journal", StringComparison.OrdinalIgnoreCase))
        {
            NwCreature? creature = caller.LoginCreature;
            if (creature == null) return;

            creature.GetObjectVariable<LocalVariableInt>("no_journal").Value = 0;
            caller.SendServerMessage("Codex will now open with the journal again.", ColorConstants.Lime);
            return;
        }

        CommandResult result = await _codex.OpenCodexAsync(caller);
        if (!result.Success)
        {
            caller.SendServerMessage(result.ErrorMessage ?? "Unable to open the codex.", ColorConstants.Orange);
        }
    }
}
