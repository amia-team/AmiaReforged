using AmiaReforged.PwEngine.Features.Chat.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
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
    public string Description => "Opens your codex (Knowledge, Quests, Notes, Reputation)";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM)
        {
            caller.SendServerMessage("The codex is not available for DMs at this time.", ColorConstants.Orange);
            return;
        }

        CommandResult result = await _codex.OpenCodexAsync(caller);
        if (!result.Success)
        {
            caller.SendServerMessage(result.ErrorMessage ?? "Unable to open the codex.", ColorConstants.Orange);
        }
    }
}
