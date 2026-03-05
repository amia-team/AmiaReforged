using AmiaReforged.PwEngine.Features.Chat.Commands;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex;

/// <summary>
/// Opens the player codex via <c>./codex</c>. Players only — DMs are rejected.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class CodexCommand : IChatCommand
{
    private readonly WindowDirector _windowDirector;

    public CodexCommand(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
    }

    public string Command => "./codex";
    public string Description => "Opens your codex (Knowledge, Quests, Notes, Reputation)";
    public string AllowedRoles => "Player";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM)
        {
            caller.SendServerMessage("The codex is not available for DMs at this time.", ColorConstants.Orange);
            return Task.CompletedTask;
        }

        if (_windowDirector.IsWindowOpen(caller, typeof(PlayerCodexPresenter)))
        {
            caller.SendServerMessage("Your codex is already open.", ColorConstants.Orange);
            return Task.CompletedTask;
        }

        PlayerCodexView view = new(caller);
        _windowDirector.OpenWindow(view.Presenter);

        return Task.CompletedTask;
    }
}
