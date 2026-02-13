using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Deprecated. The note/map-pin command no longer functions.
/// Kept as a stub so that players who type ./note or f_note get a clear message.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class NoteCommand : IChatCommand
{
    public string Command => "./note";
    public string Description => "(Deprecated) Map pin system";
    public string AllowedRoles => "Player";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        caller.SendServerMessage("This command does not do anything anymore.", ColorConstants.Orange);
        return Task.CompletedTask;
    }
}
