using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class OpenPlayerToolsCommand : IChatCommand
{
    [Inject] private WindowManager WindowManager { get; set; }

    public string Command { get; } = "./playertools";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if (caller.IsDM) return Task.CompletedTask;

        caller.SendServerMessage(
            "This command has been deprecated. Use the Player Tools feat (has a wrench icon) from your class's radial menu instead.", ColorConstants.Yellow);

        return Task.CompletedTask;
    }
}